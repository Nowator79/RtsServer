using System.Text;
using System.Text.Json;
using RtsServer.App.Adapters;
using RtsServer.App.Battle.Dto;
using RtsServer.App.Battle.MapBattle;
using RtsServer.App.Battle.Navigator;
using RtsServer.App.Battle.Units;
using RtsServer.App.FileSystem.Dto;

namespace RtsServer.App.Battle.Sim
{
    /// <summary>
    /// Толпа на основной карте: одна точка → другая, полный след клеток.
    ///   dotnet run --project Server/RtsServer --no-launch-profile -- --sim-crowd
    /// Отчёт: Server/crowd-trace/report.txt
    /// </summary>
    public static class CrowdMotionProbe
    {
        private const double TickDt = 0.05;
        private const int MaxTicks = 5000;
        private const int Crowd = 16;

        public static int Run()
        {
            string reportDir = Path.GetFullPath(Path.Combine(FindRepoMaps(), "..", "..", "crowd-trace"));
            Directory.CreateDirectory(reportDir);
            string reportPath = Path.Combine(reportDir, "report.txt");
            string csvPath = Path.Combine(reportDir, "trace.csv");

            var log = new StringBuilder();
            void Line(string s)
            {
                Console.WriteLine(s);
                log.AppendLine(s);
            }

            try
            {
                string mapFile = Path.Combine(FindRepoMaps(), "demo_mountain.json");
                Line($"map={mapFile}");
                Map map = LoadMap(mapFile);
                map.GetArrayMap();
                Line($"size={map.Width}x{map.Length}");

                if (!TryPickRoute(map, out Vector2Int spawn, out Vector2Int goal, out int probeLen))
                {
                    Line("FAIL: no walkable 4x4 pair with a path");
                    File.WriteAllText(reportPath, log.ToString());
                    return 1;
                }

                Line($"spawnCorner={spawn} goal={goal} probePathLen={probeLen}");

                Game game = Game.CreateSimulation();
                game.SetMap(map);
                game.TimeSystem.EnableFixedStep(TickDt);

                List<Unit> units = new();
                for (int i = 0; i < Crowd; i++)
                {
                    int x = spawn.X + (i % 4);
                    int y = spawn.Y + (i / 4);
                    var tank = new TankT1(new Vector2Float(x, y), playerOwner: 1);
                    game.UnitsForAdd.Add(tank);
                    units.Add(tank);
                }

                game.SimulationTick();
                GroupMoveAssigner.IssueMove(units, goal, attackMove: false, game.Map!);

                var trace = new Dictionary<int, List<Vector2Int>>();
                var stallRun = new Dictionary<int, int>();
                var maxStall = new Dictionary<int, int>();
                var awaySteps = new Dictionary<int, int>();
                var backtracks = new Dictionary<int, int>();
                var stoppedTicks = new Dictionary<int, int>();
                var movingTicks = new Dictionary<int, int>();
                var lastCell = new Dictionary<int, Vector2Int>();
                var lastDelta = new Dictionary<int, (int x, int y)>();
                var arrivedTick = new Dictionary<int, int>();
                int overlapTicks = 0;
                int maxOnOneCell = 1;

                foreach (Unit u in units)
                {
                    trace[u.Id] = new List<Vector2Int> { u.Position.ToInt() };
                    stallRun[u.Id] = 0;
                    maxStall[u.Id] = 0;
                    awaySteps[u.Id] = 0;
                    backtracks[u.Id] = 0;
                    stoppedTicks[u.Id] = 0;
                    movingTicks[u.Id] = 0;
                    lastCell[u.Id] = u.Position.ToInt();
                    lastDelta[u.Id] = (0, 0);
                    List<Vector2Int> path = u.PathRoute?.ToList() ?? new();
                    Line($"issue id={u.Id} from={u.Position.ToInt()} dest={u.TargetPosition} pathLen={path.Count} head={Fmt(path.Take(12))}");
                }

                using var csv = new StreamWriter(csvPath, false, Encoding.UTF8);
                csv.WriteLine("tick,id,x,y,cellX,cellY,destX,destY,moving,speed,pathLeft");

                int endTick = MaxTicks;
                for (int tick = 1; tick <= MaxTicks; tick++)
                {
                    game.SimulationTick();

                    var occ = new Dictionary<Vector2Int, int>();
                    bool allNear = true;
                    foreach (Unit u in units)
                    {
                        Vector2Int cell = u.Position.ToInt();
                        occ.TryGetValue(cell, out int n);
                        occ[cell] = n + 1;

                        if (tick % 10 == 0)
                        {
                            csv.WriteLine($"{tick},{u.Id},{u.Position.X:0.00},{u.Position.Y:0.00},{cell.X},{cell.Y},{u.TargetPosition.X},{u.TargetPosition.Y},{(u.IsMoving ? 1 : 0)},{u.CurrentSpeed:0.00},{u.PathRoute?.Count ?? 0}");
                        }

                        bool near = Chebyshev(cell, u.TargetPosition) <= 1 && u.IsParked;
                        if (near)
                        {
                            arrivedTick.TryAdd(u.Id, tick);
                        }
                        else
                        {
                            allNear = false;
                            if (u.CurrentSpeed < 0.05)
                                stoppedTicks[u.Id]++;
                            else
                                movingTicks[u.Id]++;
                        }

                        if (!cell.Equals(lastCell[u.Id]))
                        {
                            int dx = cell.X - lastCell[u.Id].X;
                            int dy = cell.Y - lastCell[u.Id].Y;
                            int before = Chebyshev(lastCell[u.Id], u.TargetPosition);
                            int after = Chebyshev(cell, u.TargetPosition);
                            if (after > before)
                                awaySteps[u.Id]++;

                            (int px, int py) = lastDelta[u.Id];
                            if ((px != 0 || py != 0) && dx * px + dy * py < 0)
                                backtracks[u.Id]++;

                            lastDelta[u.Id] = (dx, dy);
                            trace[u.Id].Add(cell);
                            stallRun[u.Id] = 0;
                            lastCell[u.Id] = cell;
                        }
                        else if (!near)
                        {
                            stallRun[u.Id]++;
                            maxStall[u.Id] = Math.Max(maxStall[u.Id], stallRun[u.Id]);
                        }
                    }

                    int peak = occ.Values.DefaultIfEmpty(1).Max();
                    if (peak > 1)
                        overlapTicks++;
                    maxOnOneCell = Math.Max(maxOnOneCell, peak);

                    if (allNear)
                    {
                        endTick = tick;
                        break;
                    }
                }

                Line("");
                Line($"ticks={endTick} simTime={endTick * TickDt:0.0}s overlapTicks={overlapTicks} maxOnOneCell={maxOnOneCell}");
                Line("id from dest cells walked away backtrack revisits maxStallSec stopSec moveSec arrivedSec final");

                int notArrived = 0;
                int totalAway = 0;
                int totalBack = 0;
                int totalRevisit = 0;
                foreach (Unit u in units.OrderBy(u => u.Id))
                {
                    List<Vector2Int> cells = trace[u.Id];
                    int revisits = CountRevisits(cells);
                    int walked = Math.Max(0, cells.Count - 1);
                    totalAway += awaySteps[u.Id];
                    totalBack += backtracks[u.Id];
                    totalRevisit += revisits;
                    bool arrived = arrivedTick.ContainsKey(u.Id);
                    if (!arrived) notArrived++;
                    string arr = arrived ? (arrivedTick[u.Id] * TickDt).ToString("0.0") : "-";
                    Line(
                        $"{u.Id} {cells[0]} {u.TargetPosition} {walked} " +
                        $"away={awaySteps[u.Id]} back={backtracks[u.Id]} rev={revisits} " +
                        $"stall={maxStall[u.Id] * TickDt:0.0}s stop={stoppedTicks[u.Id] * TickDt:0.0}s move={movingTicks[u.Id] * TickDt:0.0}s " +
                        $"arr={arr}s final={u.Position.ToInt()} parked={u.IsParked} wp={u.GetCurrentWaypoint()} left={u.PathRoute?.Count ?? 0}");
                    Line("  trail " + Fmt(cells));
                }

                var finals = units.GroupBy(u => u.Position.ToInt()).Where(g => g.Count() > 1).ToList();
                Line("");
                Line($"notArrived={notArrived} totalAwaySteps={totalAway} totalBacktracks={totalBack} totalRevisits={totalRevisit} stackedFinalCells={finals.Count}");
                foreach (var g in finals)
                    Line($"  STACK {g.Key} x{g.Count()}");

                File.WriteAllText(reportPath, log.ToString());
                Line($"report={reportPath}");
                return notArrived > 0 || totalBack > Crowd || maxOnOneCell > 1 ? 2 : 0;
            }
            catch (Exception ex)
            {
                Line($"FAIL {ex}");
                File.WriteAllText(reportPath, log.ToString());
                return 1;
            }
        }

        private static int CountRevisits(List<Vector2Int> cells)
        {
            int n = 0;
            var seen = new HashSet<Vector2Int>();
            foreach (Vector2Int c in cells)
            {
                if (!seen.Add(c))
                    n++;
            }
            return n;
        }

        private static string Fmt(IEnumerable<Vector2Int> cells)
        {
            var list = cells.ToList();
            if (list.Count <= 40)
                return string.Join(">", list);
            return string.Join(">", list.Take(20)) + " ... " + string.Join(">", list.Skip(list.Count - 8));
        }

        private static bool TryPickRoute(Map map, out Vector2Int spawn, out Vector2Int goal, out int probeLen)
        {
            spawn = default;
            goal = default;
            probeLen = 0;
            var blocks = new List<Vector2Int>();
            for (int y = 1; y < map.Length - 5; y++)
            for (int x = 1; x < map.Width - 5; x++)
            {
                if (BlockOk(map, x, y))
                    blocks.Add(new Vector2Int(x, y));
            }

            if (blocks.Count < 2)
                return false;

            var probe = new TankT1(new Vector2Float(0, 0), 1);
            foreach (Vector2Int a in blocks)
            {
                foreach (Vector2Int b in blocks)
                {
                    if (Chebyshev(a, b) < 28)
                        continue;
                    Vector2Int from = new(a.X + 1, a.Y + 1);
                    Vector2Int to = new(b.X + 1, b.Y + 1);
                    using var nav = new NavWave(map, from, to, probe);
                    nav.Run();
                    if (nav.IsFail)
                        continue;
                    int len = nav.GetRoutePath().Count;
                    if (len < 24)
                        continue;
                    spawn = a;
                    goal = to;
                    probeLen = len;
                    return true;
                }
            }

            return false;
        }

        private static bool BlockOk(Map map, int x, int y)
        {
            var tiles = map.GetArrayMap();
            int h0 = tiles[x, y].Height;
            for (int dy = 0; dy < 4; dy++)
            for (int dx = 0; dx < 4; dx++)
            {
                var t = tiles[x + dx, y + dy];
                if (!NavHelper.IsWalkableHeight(t))
                    return false;
                if (Math.Abs(t.Height - h0) > 1)
                    return false;
            }
            return true;
        }

        private static Map LoadMap(string file)
        {
            string json = File.ReadAllText(file);
            FMap dto = JsonSerializer.Deserialize<FMap>(json);
            return MapAdapter.Get(dto);
        }

        private static string FindRepoMaps()
        {
            string[] candidates =
            {
                @"D:\Projects\WorkProjects\MY\MilitaryCompany\Server\RtsServer\Maps",
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Maps")),
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "Maps")),
            };
            foreach (string c in candidates)
            {
                if (File.Exists(Path.Combine(c, "demo_mountain.json")))
                    return c;
            }
            throw new FileNotFoundException("demo_mountain.json");
        }

        private static int Chebyshev(Vector2Int a, Vector2Int b) =>
            Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));
    }
}
