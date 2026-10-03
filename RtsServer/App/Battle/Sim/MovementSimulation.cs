using RtsServer.App.Battle.Dto;
using RtsServer.App.Battle.MapBattle;
using RtsServer.App.Battle.MapBattle.ChunksType;
using RtsServer.App.Battle.Navigator;
using RtsServer.App.Battle.Units;

namespace RtsServer.App.Battle.Sim
{
    /// <summary>
    /// Headless прогон движения. Запуск:
    ///   dotnet run --project Server/RtsServer --no-launch-profile -- --sim-movement
    /// </summary>
    public static class MovementSimulation
    {
        private const double TickDt = 0.05;
        private const int MaxTicks = 2400;
        /// <summary>Позиция должна быть близко к координате клетки (целое X/Y).</summary>
        private const double MaxOffGrid = 0.35;

        public static int Run()
        {
            Console.WriteLine("=== MovementSimulation ===");
            int failed = 0;
            failed += RunScenario("straight_group", ScenarioStraightGroup) ? 0 : 1;
            failed += RunScenario("obstacle_detour", ScenarioObstacleDetour) ? 0 : 1;
            failed += RunScenario("grid_routes", ScenarioGridRoutes) ? 0 : 1;
            Console.WriteLine();
            Console.WriteLine(failed == 0 ? "ALL SCENARIOS PASSED" : $"FAILED SCENARIOS: {failed}");
            return failed == 0 ? 0 : 1;
        }

        private static bool RunScenario(string name, Func<ScenarioResult> scenario)
        {
            Console.WriteLine();
            Console.WriteLine($"--- {name} ---");
            try
            {
                ScenarioResult result = scenario();
                foreach (string line in result.Log)
                    Console.WriteLine(line);

                if (result.Ok)
                {
                    Console.WriteLine($"PASS {name}");
                    return true;
                }

                Console.WriteLine($"FAIL {name}: {result.FailReason}");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FAIL {name}: {ex.GetType().Name}: {ex.Message}");
                return false;
            }
        }

        private sealed class ScenarioResult
        {
            public bool Ok;
            public string FailReason = "";
            public List<string> Log = new();
        }

        private static ScenarioResult ScenarioStraightGroup()
        {
            var result = new ScenarioResult();
            Game game = CreateGameWithMap(BuildOpenMap(40, 20));
            List<Unit> units = SpawnTanks(game, count: 4, x: 3, y0: 8);
            game.SimulationTick();
            GroupMoveAssigner.IssueMove(units, new Vector2Int(30, 10), attackMove: false, game.Map!);
            return SimulateAndValidate(game, units, result);
        }

        private static ScenarioResult ScenarioObstacleDetour()
        {
            var result = new ScenarioResult();
            Map map = BuildOpenMap(40, 24);
            for (int y = 0; y < 18; y++)
                SetBlocked(map, 18, y);

            Game game = CreateGameWithMap(map);
            List<Unit> units = SpawnTanks(game, count: 3, x: 4, y0: 6);
            game.SimulationTick();
            GroupMoveAssigner.IssueMove(units, new Vector2Int(32, 8), attackMove: false, game.Map!);
            return SimulateAndValidate(game, units, result);
        }

        /// <summary>Маршруты только по соседним клеткам; после тиков — на сетке, без стака в одной клетке.</summary>
        private static ScenarioResult ScenarioGridRoutes()
        {
            var result = new ScenarioResult();
            Game game = CreateGameWithMap(BuildOpenMap(36, 20));
            List<Unit> units = SpawnTanks(game, count: 5, x: 2, y0: 6);
            game.SimulationTick();
            GroupMoveAssigner.IssueMove(units, new Vector2Int(28, 10), attackMove: false, game.Map!);

            int badRoutes = 0;
            foreach (Unit u in units)
            {
                List<Vector2Int> path = u.PathRoute?.ToList() ?? new List<Vector2Int>();
                int gaps = CountPathGaps(path);
                result.Log.Add($"unit {u.Id} pathLen={path.Count} gaps={gaps} sample={string.Join(">", path.Take(10))}");
                if (path.Count < 2)
                {
                    badRoutes++;
                    result.Log.Add("  BAD: path too short (string-pull / no grid steps?)");
                }
                if (gaps > 0)
                    badRoutes++;
            }

            if (badRoutes > 0)
            {
                result.Ok = false;
                result.FailReason = $"{badRoutes} routes violate cell-adjacency / length";
                return result;
            }

            // Прогоним и проверим on-grid.
            return SimulateAndValidate(game, units, result);
        }

        private static ScenarioResult SimulateAndValidate(Game game, List<Unit> units, ScenarioResult result)
        {
            Dictionary<int, Vector2Int> lastCell = units.ToDictionary(u => u.Id, u => u.Position.ToInt());
            Dictionary<int, int> offGridHits = units.ToDictionary(u => u.Id, _ => 0);
            Dictionary<int, int> illegalSteps = units.ToDictionary(u => u.Id, _ => 0);
            Dictionary<int, int> maxStall = units.ToDictionary(u => u.Id, _ => 0);
            Dictionary<int, int> stall = units.ToDictionary(u => u.Id, _ => 0);

            foreach (Unit u in units)
            {
                List<Vector2Int> path = u.PathRoute?.ToList() ?? new List<Vector2Int>();
                int gaps = CountPathGaps(path);
                result.Log.Add($"t=0 unit {u.Id} cell={u.Position.ToInt()} dest={u.TargetPosition} pathLen={path.Count} gaps={gaps}");
                if (gaps > 0)
                {
                    result.Ok = false;
                    result.FailReason = $"unit {u.Id} issued non-adjacent path";
                    return result;
                }
            }

            for (int tick = 1; tick <= MaxTicks; tick++)
            {
                game.SimulationTick();
                double t = game.TimeSystem.GetTime();
                bool allArrived = true;

                foreach (Unit u in units)
                {
                    Vector2Int cell = u.Position.ToInt();
                    // Off-grid только у стоячих: центр клетки = целые (X,Y) — как на клиенте.
                    if (u.IsParked)
                    {
                        if (Math.Abs(u.Position.X - cell.X) > MaxOffGrid || Math.Abs(u.Position.Y - cell.Y) > MaxOffGrid)
                            offGridHits[u.Id]++;
                    }

                    int step = Chebyshev(cell, lastCell[u.Id]);
                    if (step > 1)
                    {
                        illegalSteps[u.Id]++;
                        result.Log.Add($"t={t:0.00} unit {u.Id} ILLEGAL STEP {lastCell[u.Id]}->{cell}");
                    }

                    bool atDest = Chebyshev(cell, u.TargetPosition) <= 1;
                    if (!atDest && cell.Equals(lastCell[u.Id]))
                    {
                        stall[u.Id]++;
                        maxStall[u.Id] = Math.Max(maxStall[u.Id], stall[u.Id]);
                    }
                    else
                    {
                        stall[u.Id] = 0;
                        lastCell[u.Id] = cell;
                    }

                    if (!atDest)
                        allArrived = false;
                }

                if (tick % 100 == 0)
                {
                    foreach (Unit u in units)
                        result.Log.Add($"t={t:0.00} unit {u.Id} pos=({u.Position.X:0.00},{u.Position.Y:0.00}) cell={u.Position.ToInt()} pathLeft={u.PathRoute?.Count ?? 0}");
                }

                if (allArrived)
                {
                    result.Log.Add($"Arrived t={t:0.00}");
                    break;
                }
            }

            // Стаки: две стоячие машины в одной клетке.
            int stacks = 0;
            var parkedCells = new Dictionary<Vector2Int, int>();
            foreach (Unit u in units)
            {
                if (!u.IsParked) continue;
                Vector2Int c = u.Position.ToInt();
                parkedCells.TryGetValue(c, out int n);
                parkedCells[c] = n + 1;
            }
            stacks = parkedCells.Count(kv => kv.Value > 1);

            int notArrived = units.Count(u => Chebyshev(u.Position.ToInt(), u.TargetPosition) > 1);
            int offGridUnits = offGridHits.Count(kv => kv.Value > 20);
            int jumpUnits = illegalSteps.Count(kv => kv.Value > 0);

            foreach (Unit u in units)
            {
                result.Log.Add(
                    $"final unit {u.Id} pos=({u.Position.X:0.00},{u.Position.Y:0.00}) cell={u.Position.ToInt()} " +
                    $"dest={u.TargetPosition} dist={Chebyshev(u.Position.ToInt(), u.TargetPosition)} " +
                    $"offGridTicks={offGridHits[u.Id]} illegalSteps={illegalSteps[u.Id]} maxStall={maxStall[u.Id]}");
            }

            if (jumpUnits > 0)
            {
                result.Ok = false;
                result.FailReason = $"{jumpUnits} units skipped cells (cheb>1 per tick)";
                return result;
            }

            if (offGridUnits > 0)
            {
                result.Ok = false;
                result.FailReason = $"{offGridUnits} units spent many ticks between cells (off-grid)";
                return result;
            }

            if (stacks > 0)
            {
                result.Ok = false;
                result.FailReason = $"{stacks} cells have stacked parked units";
                return result;
            }

            if (notArrived > 0)
            {
                result.Ok = false;
                result.FailReason = $"{notArrived} units did not reach destination cell";
                return result;
            }

            result.Ok = true;
            return result;
        }

        private static List<Unit> SpawnTanks(Game game, int count, int x, int y0)
        {
            List<Unit> units = new();
            for (int i = 0; i < count; i++)
            {
                var tank = new TankT1(new Vector2Float(x, y0 + i), playerOwner: 1);
                game.UnitsForAdd.Add(tank);
                units.Add(tank);
            }
            return units;
        }

        private static Game CreateGameWithMap(Map map)
        {
            Game game = Game.CreateSimulation();
            game.SetMap(map);
            game.TimeSystem.EnableFixedStep(TickDt);
            return game;
        }

        private static Map BuildOpenMap(int width, int length)
        {
            var map = new Map(width, length);
            map.SetCode("sim_open");
            for (int x = 0; x < width; x++)
            for (int y = 0; y < length; y++)
                map.Chunks.Add(new ChunkBase(id: 1, height: 1));
            map.GetArrayMap();
            return map;
        }

        private static void SetBlocked(Map map, int x, int y)
        {
            map.GetArrayMap()[x, y].Height = 0;
            map.GetArrayMap()[x, y].Id = 0;
        }

        private static int CountPathGaps(List<Vector2Int> path)
        {
            int gaps = 0;
            for (int i = 1; i < path.Count; i++)
            {
                if (Chebyshev(path[i - 1], path[i]) > 1)
                    gaps++;
            }
            return gaps;
        }

        private static int Chebyshev(Vector2Int a, Vector2Int b) =>
            Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));
    }
}
