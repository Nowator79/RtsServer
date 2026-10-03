using RtsServer.App.Battle.Constructions;
using RtsServer.App.Battle.Dto;
using RtsServer.App.Battle.Interfaces;
using RtsServer.App.Battle.Navigator;
using RtsServer.App.Battle.Units;

namespace RtsServer.App.Battle.Ai
{
    /// <summary>
    /// ИИ: плотная база без раннего стопа, юниты со счёта ≥500 (случайно),
    /// редкие волны атаки на ближайшего врага.
    /// </summary>
    public class BasicExpandAndAttackAi : IPlayerAi
    {
        private const int AttackArmyThreshold = 12;
        private const double BuildInterval = 1.8;
        private const double ProduceInterval = 1.5;
        /// <summary>Реже — иначе pathfinding спамит и юниты «тупят».</summary>
        private const double AttackInterval = 22.0;
        private const int MinProduceResources = 500;

        /// <summary>Плотная база: 1 клетка зазора между footprints.</summary>
        private const int MinBuildingGap = 1;
        private const int SiteMinRadius = 2;
        private const int SiteMaxRadius = 22;

        private const int MaxPowerStations = 20;
        private const int TargetSupplyCenters = 10;
        private const int MaxBarracks = 2;
        private const int MaxMilitaryFactories = 2;
        private const int MaxAirFactories = 1;
        private const int TargetAntiAirTurrets = 3;

        private static readonly string[] InfantryPool = ["Soldier", "Grenadier"];
        private static readonly string[] VehiclePool = ["Baggy", "TankT1", "TankAa"];

        private readonly Random _rng = new();
        private double _buildCd;
        private double _produceCd;
        private double _attackCd;

        public void Tick(Game game, Player player, double deltaSeconds)
        {
            if (game == null || player == null || deltaSeconds <= 0)
                return;
            if (player.PlayerState != Player.PlayerStateType.Playing)
                return;

            _buildCd -= deltaSeconds;
            _produceCd -= deltaSeconds;
            _attackCd -= deltaSeconds;

            if (_buildCd <= 0)
            {
                _buildCd = BuildInterval;
                TryBuildSomething(game, player);
            }

            if (_produceCd <= 0)
            {
                _produceCd = ProduceInterval;
                TryProduce(game, player);
            }

            if (_attackCd <= 0)
            {
                _attackCd = AttackInterval;
                TryAttack(game, player);
            }
        }

        private void TryBuildSomething(Game game, Player player)
        {
            string? code = PickNextBuilding(game, player);
            if (code == null)
                return;

            if (!TryFindDenseBuildSite(game, player, code, out Vector2Int site))
                return;

            int cost = ConstructionFactory.GetBuildCost(code);
            if (player.GetState().Resources < cost)
                return;

            game.TryBuildConstruction(code, site, player.Id);
        }

        private static string? PickNextBuilding(Game game, Player player)
        {
            var state = player.GetState();
            int power = Count(game, player, "PowerStation");
            int supply = Count(game, player, "SupplyCenter");
            int barracks = Count(game, player, "Barracks");
            int mil = Count(game, player, "MilitaryFactory");
            int air = Count(game, player, "AirFactory");
            int aa = Count(game, player, "AntiAirTurret");

            int freeEnergy = state.LimitEnergy - state.UseEnergy;

            // Всегда: не хватает тока → электростанция.
            if (NeedsPower(freeEnergy) && power < MaxPowerStations)
                return "PowerStation";

            // 1) Очередь: 10 центров снабжения.
            if (supply < TargetSupplyCenters)
            {
                if (NeedsPower(freeEnergy - 25) && power < MaxPowerStations)
                    return "PowerStation";
                return "SupplyCenter";
            }

            // 2) Барак → военный завод → аэродром.
            if (barracks < 1)
            {
                if (NeedsPower(freeEnergy - 25) && power < MaxPowerStations)
                    return "PowerStation";
                return "Barracks";
            }

            if (mil < 1)
            {
                if (NeedsPower(freeEnergy - 40) && power < MaxPowerStations)
                    return "PowerStation";
                return "MilitaryFactory";
            }

            if (air < MaxAirFactories)
            {
                if (NeedsPower(freeEnergy - 50) && power < MaxPowerStations)
                    return "PowerStation";
                return "AirFactory";
            }

            if (barracks < MaxBarracks)
            {
                if (NeedsPower(freeEnergy - 25) && power < MaxPowerStations)
                    return "PowerStation";
                return "Barracks";
            }

            if (mil < MaxMilitaryFactories)
            {
                if (NeedsPower(freeEnergy - 40) && power < MaxPowerStations)
                    return "PowerStation";
                return "MilitaryFactory";
            }

            // 3) ПВО — 3 штуки.
            if (aa < TargetAntiAirTurrets)
            {
                if (NeedsPower(freeEnergy - 35) && power < MaxPowerStations)
                    return "PowerStation";
                return "AntiAirTurret";
            }

            // Дальше — запас энергии / ещё снабжение при забитом складе.
            bool storageTight = state.MaxResources > 0
                && state.Resources >= state.MaxResources * 0.75;
            if (storageTight && supply < TargetSupplyCenters + 4)
                return "SupplyCenter";

            if (freeEnergy < 60 && power < MaxPowerStations)
                return "PowerStation";

            return null;
        }

        /// <summary>Мало свободной энергии — срочно нужна станция.</summary>
        private static bool NeedsPower(int freeEnergy) => freeEnergy < 30;

        private static int Count(Game game, Player player, string code) =>
            game.Constructions.Count(c =>
                c.OwnerId == player.Id && !c.IsDestroyed && c.Code == code)
            + game.ConstructionsForAdd.Count(c =>
                c.OwnerId == player.Id && !c.IsDestroyed && c.Code == code);

        /// <summary>Площадка ближе к HQ / уже стоящим зданиям (компактная база).</summary>
        private static bool TryFindDenseBuildSite(Game game, Player player, string code, out Vector2Int site)
        {
            site = default;
            Construction? hq = game.Constructions.FirstOrDefault(c =>
                c.OwnerId == player.Id && !c.IsDestroyed && c.Code == "Headquarters");
            if (hq == null)
                return false;

            Vector2Int origin = hq.Position.ToInt();
            Vector2Int size = ConstructionFactory.GetSize(code);
            int sx = Math.Max(1, size.X);
            int sy = Math.Max(1, size.Y);

            Vector2Int? best = null;
            double bestScore = double.MaxValue;

            int rMax = Math.Min(SiteMaxRadius, Game.BuildRangeCells);
            for (int r = SiteMinRadius; r <= rMax; r++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    for (int dy = -r; dy <= r; dy++)
                    {
                        if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != r)
                            continue;

                        Vector2Int candidate = new(origin.X + dx, origin.Y + dy);
                        if (!game.CanPlaceConstruction(code, candidate, player.Id, out _))
                            continue;
                        if (!HasDenseGap(game, player, candidate, sx, sy))
                            continue;

                        double distHq = Math.Max(Math.Abs(dx), Math.Abs(dy));
                        double nearest = NearestOwnBuildingDistance(game, player, candidate, sx, sy);
                        // Ближе к базе и к соседям — лучше (плотность).
                        double score = distHq * 1.2 + nearest * 0.35;
                        if (score < bestScore)
                        {
                            bestScore = score;
                            best = candidate;
                        }
                    }
                }

                // На первом кольце с валидным местом можно брать — база растёт плотно.
                if (best.HasValue && r >= SiteMinRadius + 1)
                {
                    site = best.Value;
                    return true;
                }
            }

            if (best.HasValue)
            {
                site = best.Value;
                return true;
            }

            // Фоллбек без зазора.
            for (int r = 1; r <= rMax; r++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    for (int dy = -r; dy <= r; dy++)
                    {
                        if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != r)
                            continue;
                        Vector2Int candidate = new(origin.X + dx, origin.Y + dy);
                        if (!game.CanPlaceConstruction(code, candidate, player.Id, out _))
                            continue;
                        site = candidate;
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool HasDenseGap(Game game, Player player, Vector2Int pos, int sx, int sy)
        {
            foreach (Construction c in game.Constructions.Concat(game.ConstructionsForAdd))
            {
                if (c.OwnerId != player.Id || c.IsDestroyed)
                    continue;
                if (FootprintGap(pos, sx, sy, c.Position.ToInt(), c.Size.X, c.Size.Y) < MinBuildingGap)
                    return false;
            }
            return true;
        }

        private static double NearestOwnBuildingDistance(
            Game game, Player player, Vector2Int pos, int sx, int sy)
        {
            double nearest = 99;
            foreach (Construction c in game.Constructions.Concat(game.ConstructionsForAdd))
            {
                if (c.OwnerId != player.Id || c.IsDestroyed)
                    continue;
                double g = FootprintGap(pos, sx, sy, c.Position.ToInt(), c.Size.X, c.Size.Y);
                if (g < nearest)
                    nearest = g;
            }
            return nearest;
        }

        private static double FootprintGap(
            Vector2Int aPos, int aSx, int aSy,
            Vector2Int bPos, int bSx, int bSy)
        {
            int aMaxX = aPos.X + aSx - 1;
            int aMaxY = aPos.Y + aSy - 1;
            int bMaxX = bPos.X + bSx - 1;
            int bMaxY = bPos.Y + bSy - 1;

            int gapX = 0;
            if (aMaxX < bPos.X) gapX = bPos.X - aMaxX - 1;
            else if (bMaxX < aPos.X) gapX = aPos.X - bMaxX - 1;

            int gapY = 0;
            if (aMaxY < bPos.Y) gapY = bPos.Y - aMaxY - 1;
            else if (bMaxY < aPos.Y) gapY = aPos.Y - bMaxY - 1;

            if (aMaxX >= bPos.X && bMaxX >= aPos.X && aMaxY >= bPos.Y && bMaxY >= aPos.Y)
                return 0;

            bool overlapX = aMaxX >= bPos.X && bMaxX >= aPos.X;
            bool overlapY = aMaxY >= bPos.Y && bMaxY >= aPos.Y;
            if (overlapX) return gapY;
            if (overlapY) return gapX;
            return Math.Max(gapX, gapY);
        }

        private void TryProduce(Game game, Player player)
        {
            var state = player.GetState();
            if (state.LimitEnergy - state.UseEnergy < 0)
                return;
            if (state.Resources < MinProduceResources)
                return;

            foreach (Construction c in game.Constructions)
            {
                if (c.OwnerId != player.Id || c.IsDestroyed || !c.IsBuilt || !c.IsPowered)
                    continue;
                if (c is not IUnitFactory factory)
                    continue;

                string? unitCode = c switch
                {
                    BarracksConstruction => PickRandomFromPool(player, InfantryPool, BarracksConstruction.TryGetUnitRecipe),
                    MilitaryFactoryConstruction => PickRandomFromPool(player, VehiclePool, MilitaryFactoryConstruction.TryGetUnitRecipe),
                    AirFactoryConstruction => "AirUnit",
                    _ => null
                };
                if (unitCode == null)
                    continue;
                if (!factory.TryGetRecipe(unitCode, out int cost, out _))
                    continue;
                if (player.GetState().Resources < Math.Max(cost, MinProduceResources))
                    continue;

                game.TryProduceUnit(c.Id, unitCode, player.Id);
            }
        }

        private delegate bool TryRecipe(string unitCode, out int cost, out float buildSeconds);

        /// <summary>Случайный юнит из пула, на кого хватает денег.</summary>
        private string? PickRandomFromPool(Player player, string[] pool, TryRecipe tryRecipe)
        {
            int res = player.GetState().Resources;
            List<string> affordable = new(pool.Length);
            foreach (string code in pool)
            {
                if (!tryRecipe(code, out int cost, out _))
                    continue;
                if (res >= cost)
                    affordable.Add(code);
            }

            if (affordable.Count == 0)
                return null;
            return affordable[_rng.Next(affordable.Count)];
        }

        private static void TryAttack(Game game, Player player)
        {
            List<Unit> army = game.Units
                .Where(u => u.OwnerId == player.Id && !u.IsDestroyed)
                .ToList();
            if (army.Count <= AttackArmyThreshold)
                return;

            // Не дёргаем тех, кто уже идёт attack-move — иначе путь сбрасывается каждые N секунд.
            List<Unit> available = army
                .Where(u => !u.IsAttackMoving)
                .ToList();
            if (available.Count < Math.Max(4, AttackArmyThreshold / 2))
                return;

            IAttackTarget? target = FindNearestEnemyTarget(game, player);
            if (target == null)
                return;

            Vector2Int dest = target.AimPosition.ToInt();
            int waveSize = Math.Min(available.Count, Math.Max(AttackArmyThreshold, available.Count * 2 / 3));
            List<Unit> wave = available.Take(waveSize).ToList();
            if (game.Map != null)
                GroupMoveAssigner.IssueMove(wave, dest, attackMove: true, game.Map);

            foreach (Unit u in wave)
                u.SetAttackTarget(target);
        }

        private static IAttackTarget? FindNearestEnemyTarget(Game game, Player player)
        {
            Vector2Float origin = GetOwnOrigin(game, player);

            IAttackTarget? best = null;
            double bestDist = double.MaxValue;

            foreach (Construction c in game.Constructions)
            {
                if (c.OwnerId == player.Id || c.IsDestroyed)
                    continue;
                double dist = Vector2Float.Distance(origin, c.AimPosition);
                if (dist >= bestDist)
                    continue;
                bestDist = dist;
                best = c;
            }

            foreach (Unit u in game.Units)
            {
                if (u.OwnerId == player.Id || u.IsDestroyed)
                    continue;
                double dist = Vector2Float.Distance(origin, u.AimPosition);
                if (dist >= bestDist)
                    continue;
                bestDist = dist;
                best = u;
            }

            return best;
        }

        private static Vector2Float GetOwnOrigin(Game game, Player player)
        {
            Construction? hq = game.Constructions.FirstOrDefault(c =>
                c.OwnerId == player.Id && !c.IsDestroyed && c.Code == "Headquarters");
            if (hq != null)
                return hq.AimPosition;

            Unit? any = game.Units.FirstOrDefault(u => u.OwnerId == player.Id && !u.IsDestroyed);
            if (any != null)
                return any.Position;

            return Vector2Float.Zero;
        }
    }
}
