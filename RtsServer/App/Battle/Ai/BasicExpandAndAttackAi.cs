using RtsServer.App.Battle.Constructions;
using RtsServer.App.Battle.Dto;
using RtsServer.App.Battle.Interfaces;
using RtsServer.App.Battle.Navigator;
using RtsServer.App.Battle.Units;

namespace RtsServer.App.Battle.Ai
{
    /// <summary>
    /// ИИ: ранняя армия (барак после 1–2 снабжений), очередь производства,
    /// волны атаки на вражеский штаб / видимых врагов.
    /// </summary>
    public class BasicExpandAndAttackAi : IPlayerAi
    {
        private const int AttackArmyThreshold = 5;
        private const double BuildInterval = 1.8;
        private const double ProduceInterval = 1.0;
        private const double AttackInterval = 10.0;

        /// <summary>Плотная база: 1 клетка зазора между footprints.</summary>
        private const int MinBuildingGap = 1;
        private const int SiteMinRadius = 2;
        private const int SiteMaxRadius = 22;

        private const int MaxPowerStations = 20;
        /// <summary>Сначала 1 снабжение + барак, потом добираем экономику.</summary>
        private const int EarlySupplyCenters = 2;
        private const int TargetSupplyCenters = 8;
        private const int MaxBarracks = 2;
        private const int MaxMilitaryFactories = 2;
        private const int MaxAirFactories = 1;
        private const int TargetAntiAirTurrets = 3;
        private const int MaxCranes = 4;
        /// <summary>Ниже этого — не штампуем войско (кроме МСЦ/экспанда).</summary>
        private const int MinArmyBank = 1000;
        /// <summary>Успешный заказ → 2 мин; неудача → быстрый ретрай.</summary>
        private const double TransporterOrderInterval = 120.0;
        private const double TransporterRetryInterval = 5.0;
        private const double ExpandDriveInterval = 1.0;
        private const int OreClusterMergeDist = 4;
        private const int DeploySearchRadius = 8;

        private static readonly string[] InfantryPool = ["Soldier", "Grenadier"];
        private static readonly string[] VehiclePool = ["Baggy", "TankT1", "TankAa", "TankArtillery"];

        private readonly Random _rng = new();
        private double _buildCd;
        private double _produceCd;
        private double _attackCd;
        private double _transporterOrderCd = 15.0; // первый чуть раньше, дальше каждые 2 мин
        private double _expandDriveCd;

        /// <summary>Кластеры руды карты (центр месторождения). Строятся один раз.</summary>
        private List<Vector2Int>? _oreClusters;

        /// <summary>Транспортник Id → клетка у целевого месторождения.</summary>
        private readonly Dictionary<int, Vector2Int> _transporterTargets = new();

        /// <summary>Цель для следующего ещё не появившегося транспортника.</summary>
        private Vector2Int? _pendingTransporterTarget;

        /// <summary>Резерв под экспанд (кэш на тик).</summary>
        private int? _expandReserveCache;

        public void Tick(Game game, Player player, double deltaSeconds)
        {
            if (game == null || player == null || deltaSeconds <= 0)
                return;
            if (player.PlayerState != Player.PlayerStateType.Playing)
                return;

            _expandReserveCache = null;

            _buildCd -= deltaSeconds;
            _produceCd -= deltaSeconds;
            _attackCd -= deltaSeconds;
            _transporterOrderCd -= deltaSeconds;
            _expandDriveCd -= deltaSeconds;

            // Зона стройки только от МСЦ — сначала разворачиваем его.
            EnsureMobileHq(game, player);

            if (_expandDriveCd <= 0)
            {
                _expandDriveCd = ExpandDriveInterval;
                DriveExpandTransporters(game, player);
            }

            if (_transporterOrderCd <= 0)
                TryOrderExpandTransporter(game, player);

            if (_buildCd <= 0)
            {
                _buildCd = BuildInterval;
                if (HasBuildZone(game, player))
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

        /// <summary>Нет MobileHq → произвести МСЦ у штаба и развернуть рядом.</summary>
        private void EnsureMobileHq(Game game, Player player)
        {
            if (HasBuildZone(game, player))
                return;

            Unit? mcc = game.Units.FirstOrDefault(u =>
                u.OwnerId == player.Id && !u.IsDestroyed && u.Code == "MobileConstructionCenter");
            if (mcc != null)
            {
                TryDeployMccNearOre(game, player, mcc);
                return;
            }

            if (IsProducingOrQueued(game, player, "MobileConstructionCenter"))
                return;

            Construction? hq = FindHq(game, player);
            if (hq == null || !hq.IsBuilt || !hq.IsPowered)
                return;
            if (hq is not IUnitFactory factory || !factory.CanProduce("MobileConstructionCenter"))
                return;
            if (!HeadquartersConstruction.TryGetUnitRecipe("MobileConstructionCenter", out int cost, out _))
                return;
            if (player.GetState().Resources < cost)
                return;

            game.TryProduceUnit(hq.Id, "MobileConstructionCenter", player.Id);
        }

        private static bool HasBuildZone(Game game, Player player) =>
            game.Constructions.Concat(game.ConstructionsForAdd).Any(c =>
                c.OwnerId == player.Id
                && !c.IsDestroyed
                && Game.IsBuildZoneProvider(c.Code));

        private static bool IsProducingOrQueued(Game game, Player player, string unitCode)
        {
            foreach (Construction c in game.Constructions)
            {
                if (c.OwnerId != player.Id || c.IsDestroyed)
                    continue;
                if (c is not IUnitProductionHost host)
                    continue;
                if (host.CurrentProductionCode == unitCode)
                    return true;
                string csv = host.ProductionQueueCsv;
                if (!string.IsNullOrEmpty(csv))
                {
                    foreach (string q in csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    {
                        if (q == unitCode)
                            return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Разворачивает МСЦ так, чтобы зона стройки накрыла руду (SupplyCenter нужен ore под footprint).
        /// </summary>
        private static void TryDeployMccNearOre(Game game, Player player, Unit mcc)
        {
            Construction? hq = FindHq(game, player);
            if (hq == null)
                return;

            Vector2Int unitCell = new((int)Math.Floor(mcc.Position.X), (int)Math.Floor(mcc.Position.Y));

            if (!TryFindMccDeploySiteNearOre(game, player, hq, out Vector2Int site))
                return;

            // Уже на целевой клетке — разворот.
            if (unitCell.X == site.X && unitCell.Y == site.Y)
            {
                if (game.CanPlaceConstruction(
                        MobileHqConstruction.Code, unitCell, player.Id, out _, forDeploy: true))
                    game.TryDeployUnit(mcc.Id, unitCell, player.Id);
                return;
            }

            // На месте и клетка уже накрывает руду — можно развернуть сразу.
            if (CountOreInBuildZone(game, unitCell) > 0
                && game.CanPlaceConstruction(
                    MobileHqConstruction.Code, unitCell, player.Id, out _, forDeploy: true)
                && Chebyshev(unitCell, site) <= 2)
            {
                game.TryDeployUnit(mcc.Id, unitCell, player.Id);
                return;
            }

            if (!mcc.IsMoving || mcc.TargetPosition.X != site.X || mcc.TargetPosition.Y != site.Y)
                mcc.SetTargetPosition(site);
        }

        /// <summary>
        /// Лучшая клетка: максимум руды в зоне МСЦ, недалеко от HQ (до ~28 клеток).
        /// </summary>
        private static bool TryFindMccDeploySiteNearOre(
            Game game, Player player, Construction hq, out Vector2Int site)
        {
            site = default;
            Vector2Int hqCenter = new(
                hq.Position.ToInt().X + hq.Size.X / 2,
                hq.Position.ToInt().Y + hq.Size.Y / 2);

            const int maxDistFromHq = 28;
            int zone = Game.BuildRangeCells;

            Vector2Int? best = null;
            double bestScore = double.MinValue;

            // Кандидаты вокруг руды: клетка deploy в пределах zone от ore.
            if (game.Map?.Ore != null)
            {
                foreach (var (ox, oy, amount) in game.Map.Ore.Enumerate())
                {
                    if (amount <= 0)
                        continue;

                    for (int dx = -zone; dx <= zone; dx++)
                    {
                        for (int dy = -zone; dy <= zone; dy++)
                        {
                            if (Math.Max(Math.Abs(dx), Math.Abs(dy)) > zone)
                                continue;

                            Vector2Int candidate = new(ox + dx, oy + dy);
                            int distHq = Chebyshev(candidate, hqCenter);
                            if (distHq > maxDistFromHq)
                                continue;
                            if (!game.CanPlaceConstruction(
                                    MobileHqConstruction.Code, candidate, player.Id, out _, forDeploy: true))
                                continue;

                            int oreCovered = CountOreInBuildZone(game, candidate);
                            // Больше руды в зоне важнее близости к штабу.
                            double score = oreCovered * 40.0 - distHq * 1.2;
                            if (score > bestScore)
                            {
                                bestScore = score;
                                best = candidate;
                            }
                        }
                    }
                }
            }

            if (best.HasValue)
            {
                site = best.Value;
                return true;
            }

            // Нет руды / недоступна — фоллбек рядом со штабом.
            for (int r = 2; r <= 8; r++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    for (int dy = -r; dy <= r; dy++)
                    {
                        if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != r)
                            continue;
                        Vector2Int candidate = new(hqCenter.X + dx, hqCenter.Y + dy);
                        if (!game.CanPlaceConstruction(
                                MobileHqConstruction.Code, candidate, player.Id, out _, forDeploy: true))
                            continue;
                        site = candidate;
                        return true;
                    }
                }
            }

            return false;
        }

        private static int CountOreInBuildZone(Game game, Vector2Int mobileHqCell)
        {
            if (game.Map?.Ore == null)
                return 0;

            int zone = Game.BuildRangeCells;
            int count = 0;
            // 1×1 MobileHq: footprint gap ≤ zone ≈ клетки в Chebyshev zone от hq.
            for (int x = mobileHqCell.X - zone; x <= mobileHqCell.X + zone; x++)
            {
                for (int y = mobileHqCell.Y - zone; y <= mobileHqCell.Y + zone; y++)
                {
                    if (game.Map.Ore.Get(x, y) > 0)
                        count++;
                }
            }

            return count;
        }

        private static int Chebyshev(Vector2Int a, Vector2Int b) =>
            Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));

        private static Construction? FindHq(Game game, Player player) =>
            game.Constructions.FirstOrDefault(c =>
                c.OwnerId == player.Id && !c.IsDestroyed && c.Code == "Headquarters");

        private void TryBuildSomething(Game game, Player player)
        {
            string? code = PickNextBuilding(game, player);
            if (code == null)
                return;

            if (!TryFindDenseBuildSite(game, player, code, out Vector2Int site))
                return;

            int cost = ConstructionFactory.GetBuildCost(code);
            int reserve = GetExpandReserve(game, player);
            // AirFactory / Power / Supply — база и добыча; краны/ПВО не жрут резерв экспанда.
            bool isExpandGoal = code is "AirFactory" or "PowerStation" or "SupplyCenter";
            int need = isExpandGoal ? cost : cost + reserve;
            if (player.GetState().Resources < need)
                return;

            game.TryBuildConstruction(code, site, player.Id);
        }

        /// <summary>
        /// Копим на экспанд: авиазавод или транспортник важнее мусорных солдат/лишних зданий.
        /// </summary>
        private int GetExpandReserve(Game game, Player player)
        {
            if (_expandReserveCache.HasValue)
                return _expandReserveCache.Value;

            int reserve = 0;
            int air = Count(game, player, "AirFactory");
            int barracks = Count(game, player, "Barracks");
            int supply = Count(game, player, "SupplyCenter");

            if (air < MaxAirFactories && barracks >= 1 && supply >= 1)
            {
                reserve = ConstructionFactory.GetBuildCost("AirFactory");
            }
            else if (HasBuiltAirFactory(game, player)
                     && !IsProducingOrQueued(game, player, "AirTransporter")
                     && TryPickUncoveredOreTarget(game, player, out _))
            {
                if (AirFactoryConstruction.TryGetUnitRecipe("AirTransporter", out int cost, out _))
                    reserve = cost;
            }

            _expandReserveCache = reserve;
            return reserve;
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

            // Ранняя армия: снабжение → барак → авиазавод (для экспанда) → дальше экономика.
            if (supply < 1)
            {
                if (NeedsPower(freeEnergy - 25) && power < MaxPowerStations)
                    return "PowerStation";
                return "SupplyCenter";
            }

            if (barracks < 1)
            {
                if (NeedsPower(freeEnergy - 25) && power < MaxPowerStations)
                    return "PowerStation";
                return "Barracks";
            }

            if (air < MaxAirFactories)
            {
                if (NeedsPower(freeEnergy - 50) && power < MaxPowerStations)
                    return "PowerStation";
                return "AirFactory";
            }

            // Руда в зоне без добычи (дом / остров после AirDrop) — раньше кранов и 2-го завода.
            if (NeedsSupplyOnUnminedOre(game, player))
            {
                if (NeedsPower(freeEnergy - 25) && power < MaxPowerStations)
                    return "PowerStation";
                return "SupplyCenter";
            }

            if (supply < EarlySupplyCenters)
            {
                if (NeedsPower(freeEnergy - 25) && power < MaxPowerStations)
                    return "PowerStation";
                return "SupplyCenter";
            }

            if (mil < 1)
            {
                if (NeedsPower(freeEnergy - 40) && power < MaxPowerStations)
                    return "PowerStation";
                return "MilitaryFactory";
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

            if (supply < TargetSupplyCenters)
            {
                if (NeedsPower(freeEnergy - 25) && power < MaxPowerStations)
                    return "PowerStation";
                return "SupplyCenter";
            }

            if (aa < TargetAntiAirTurrets)
            {
                if (NeedsPower(freeEnergy - 35) && power < MaxPowerStations)
                    return "PowerStation";
                return "AntiAirTurret";
            }

            int cranes = Count(game, player, CraneConstruction.Code);
            if (cranes < MaxCranes && air >= 1)
            {
                if (NeedsPower(freeEnergy - 12) && power < MaxPowerStations)
                    return "PowerStation";
                return CraneConstruction.Code;
            }

            bool storageTight = state.MaxResources > 0
                && state.Resources >= state.MaxResources * 0.75;
            if (storageTight && supply < TargetSupplyCenters + 4)
                return "SupplyCenter";

            if (freeEnergy < 60 && power < MaxPowerStations)
                return "PowerStation";

            return null;
        }

        /// <summary>
        /// Заказ транспортника на месторождение без крана/зоны.
        /// При успехе — пауза 2 мин; при любой неудаче — ретрай через 5 с (не через 2 мин!).
        /// </summary>
        private void TryOrderExpandTransporter(Game game, Player player)
        {
            void RetrySoon() => _transporterOrderCd = TransporterRetryInterval;
            void OkWait() => _transporterOrderCd = TransporterOrderInterval;

            if (!HasBuiltAirFactory(game, player))
            {
                RetrySoon();
                return;
            }

            if (IsProducingOrQueued(game, player, "AirTransporter"))
            {
                // Уже в очереди — следующий заказ по длинному интервалу.
                OkWait();
                return;
            }

            if (!TryPickUncoveredOreTarget(game, player, out Vector2Int target))
            {
                RetrySoon();
                return;
            }

            if (!AirFactoryConstruction.TryGetUnitRecipe("AirTransporter", out int cost, out _))
            {
                RetrySoon();
                return;
            }

            if (player.GetState().Resources < cost)
            {
                RetrySoon();
                return;
            }

            Construction? airFactory = game.Constructions.FirstOrDefault(c =>
                c.OwnerId == player.Id
                && !c.IsDestroyed
                && c.IsBuilt
                && c.IsPowered
                && c.Code == "AirFactory");
            if (airFactory == null)
            {
                RetrySoon();
                return;
            }

            // Очередь забита самолётами — освобождаем слот.
            if (airFactory is IUnitFactory factory
                && CountFactoryOrders(airFactory) >= UnitProductionLimits.MaxOrders)
            {
                if (!factory.TryCancelOne("AirUnit") && !factory.TryCancelOne("Helicopter"))
                {
                    RetrySoon();
                    return;
                }
            }

            int before = player.GetState().Resources;
            game.TryProduceUnit(airFactory.Id, "AirTransporter", player.Id);
            if (player.GetState().Resources < before)
            {
                _pendingTransporterTarget = target;
                OkWait();
            }
            else
            {
                RetrySoon();
            }
        }

        private void DriveExpandTransporters(Game game, Player player)
        {
            EnsureOreClusters(game);
            CleanupTransporterTargets(game, player);

            foreach (Unit transporter in game.Units)
            {
                if (transporter.OwnerId != player.Id || transporter.IsDestroyed)
                    continue;
                if (transporter.Code != "AirTransporter")
                    continue;

                if (!_transporterTargets.ContainsKey(transporter.Id))
                    AssignTransporterTarget(game, player, transporter);

                if (!_transporterTargets.TryGetValue(transporter.Id, out Vector2Int site))
                    continue;

                DriveTransporterToCell(game, player, transporter, site);
            }
        }

        private void AssignTransporterTarget(Game game, Player player, Unit transporter)
        {
            if (_pendingTransporterTarget.HasValue)
            {
                _transporterTargets[transporter.Id] = _pendingTransporterTarget.Value;
                _pendingTransporterTarget = null;
                return;
            }

            if (TryPickUncoveredOreTarget(game, player, out Vector2Int target))
                _transporterTargets[transporter.Id] = target;
        }

        private void CleanupTransporterTargets(Game game, Player player)
        {
            List<int> dead = new();
            foreach (int id in _transporterTargets.Keys)
            {
                Unit? u = game.Units.FirstOrDefault(x => x.Id == id);
                if (u == null || u.IsDestroyed || u.OwnerId != player.Id || u.Code != "AirTransporter")
                    dead.Add(id);
            }

            foreach (int id in dead)
                _transporterTargets.Remove(id);
        }

        private void DriveTransporterToCell(Game game, Player player, Unit transporter, Vector2Int site)
        {
            Vector2Int unitCell = new(
                (int)Math.Floor(transporter.Position.X),
                (int)Math.Floor(transporter.Position.Y));

            if (Chebyshev(unitCell, site) <= 1
                && game.CanPlaceConstruction(
                    AirDropBaseConstruction.Code, unitCell, player.Id, out _, forDeploy: true))
            {
                game.TryDeployUnit(transporter.Id, unitCell, player.Id);
                _transporterTargets.Remove(transporter.Id);
                return;
            }

            if (!transporter.IsMoving
                || transporter.TargetPosition.X != site.X
                || transporter.TargetPosition.Y != site.Y)
            {
                transporter.SetTargetPosition(site);
            }
        }

        /// <summary>
        /// Месторождение без своего крана/зоны стройки + клетка рядом для деплоя.
        /// Уже назначенные транспортникам точки пропускаем.
        /// </summary>
        private bool TryPickUncoveredOreTarget(Game game, Player player, out Vector2Int deployCell)
        {
            deployCell = default;
            EnsureOreClusters(game);
            if (_oreClusters == null || _oreClusters.Count == 0)
                return false;

            var providers = CollectBuildZoneProviders(game, player);
            HashSet<Vector2Int> reserved = new(_transporterTargets.Values);
            if (_pendingTransporterTarget.HasValue)
                reserved.Add(_pendingTransporterTarget.Value);

            Construction? hq = FindHq(game, player);
            Vector2Int hqCenter = hq != null
                ? new(hq.Position.ToInt().X + hq.Size.X / 2, hq.Position.ToInt().Y + hq.Size.Y / 2)
                : new(0, 0);

            Vector2Int? bestDeploy = null;
            int bestDist = -1;

            foreach (Vector2Int cluster in _oreClusters)
            {
                if (OreClusterHasCraneOrZone(game, player, providers, cluster))
                    continue;
                if (!TryFindDeployCellNear(game, player, providers, cluster, out Vector2Int cell))
                    continue;
                if (reserved.Contains(cell))
                    continue;

                int dist = Chebyshev(cell, hqCenter);
                if (dist > bestDist)
                {
                    bestDist = dist;
                    bestDeploy = cell;
                }
            }

            if (!bestDeploy.HasValue)
                return false;

            deployCell = bestDeploy.Value;
            return true;
        }

        /// <summary>Покрыто: свой кран рядом или любая своя зона стройки на центре месторождения.</summary>
        private static bool OreClusterHasCraneOrZone(
            Game game,
            Player player,
            List<(Vector2Int Pos, Vector2Int Size, int Range)> providers,
            Vector2Int cluster)
        {
            if (IsCellInOwnBuildZone(providers, cluster))
                return true;

            int craneRange = CraneConstruction.BuildZoneRange;
            foreach (Construction c in game.Constructions)
            {
                if (c.OwnerId != player.Id || c.IsDestroyed || c.Code != CraneConstruction.Code)
                    continue;
                Vector2Int cranePos = c.Position.ToInt();
                if (Chebyshev(cluster, cranePos) <= craneRange + 1)
                    return true;
            }

            return false;
        }

        private static bool TryFindDeployCellNear(
            Game game,
            Player player,
            List<(Vector2Int Pos, Vector2Int Size, int Range)> providers,
            Vector2Int cluster,
            out Vector2Int cell)
        {
            cell = default;
            Vector2Int? best = null;
            int bestScore = int.MaxValue;

            for (int dx = -DeploySearchRadius; dx <= DeploySearchRadius; dx++)
            {
                for (int dy = -DeploySearchRadius; dy <= DeploySearchRadius; dy++)
                {
                    Vector2Int candidate = new(cluster.X + dx, cluster.Y + dy);
                    if (!IsLandCell(game, candidate.X, candidate.Y))
                        continue;
                    if (IsCellInOwnBuildZone(providers, candidate))
                        continue;
                    if (!game.CanPlaceConstruction(
                            AirDropBaseConstruction.Code, candidate, player.Id, out _, forDeploy: true))
                        continue;

                    int score = Math.Max(Math.Abs(dx), Math.Abs(dy));
                    if (score < bestScore)
                    {
                        bestScore = score;
                        best = candidate;
                    }
                }
            }

            if (!best.HasValue)
                return false;

            cell = best.Value;
            return true;
        }

        private void EnsureOreClusters(Game game)
        {
            // Пустой кэш не оставляем навсегда — руда могла появиться после первого тика.
            if (_oreClusters != null && _oreClusters.Count > 0)
                return;

            List<Vector2Int> built = BuildOreClusters(game);
            if (built.Count > 0 || _oreClusters == null)
                _oreClusters = built;
        }

        /// <summary>Сливает соседние клетки руды в точки-месторождения (клетка с макс. запасом).</summary>
        private static List<Vector2Int> BuildOreClusters(Game game)
        {
            var result = new List<Vector2Int>();
            if (game.Map?.Ore == null)
                return result;

            List<(int X, int Y, int Amount)> cells = new();
            foreach (var (x, y, amount) in game.Map.Ore.Enumerate())
            {
                if (amount > 0)
                    cells.Add((x, y, amount));
            }

            bool[] used = new bool[cells.Count];
            for (int i = 0; i < cells.Count; i++)
            {
                if (used[i])
                    continue;

                List<(int X, int Y, int Amount)> cluster = new();
                Queue<int> q = new();
                q.Enqueue(i);
                used[i] = true;

                while (q.Count > 0)
                {
                    int idx = q.Dequeue();
                    cluster.Add(cells[idx]);
                    for (int j = 0; j < cells.Count; j++)
                    {
                        if (used[j])
                            continue;
                        if (Math.Max(
                                Math.Abs(cells[j].X - cells[idx].X),
                                Math.Abs(cells[j].Y - cells[idx].Y)) > OreClusterMergeDist)
                            continue;
                        used[j] = true;
                        q.Enqueue(j);
                    }
                }

                (int X, int Y, int Amount) best = cluster[0];
                foreach (var c in cluster)
                {
                    if (c.Amount > best.Amount)
                        best = c;
                }

                result.Add(new Vector2Int(best.X, best.Y));
            }

            return result;
        }

        private static bool HasBuiltAirFactory(Game game, Player player) =>
            game.Constructions.Any(c =>
                c.OwnerId == player.Id
                && !c.IsDestroyed
                && c.IsBuilt
                && c.IsPowered
                && c.Code == "AirFactory");

        private static List<(Vector2Int Pos, Vector2Int Size, int Range)> CollectBuildZoneProviders(
            Game game, Player player)
        {
            var list = new List<(Vector2Int, Vector2Int, int)>(8);
            void AddRange(IEnumerable<Construction> src)
            {
                foreach (Construction existing in src)
                {
                    if (existing.OwnerId != player.Id || existing.IsDestroyed)
                        continue;
                    if (!Game.IsBuildZoneProvider(existing.Code))
                        continue;
                    list.Add((
                        existing.Position.ToInt(),
                        existing.Size,
                        Game.GetBuildZoneRange(existing.Code)));
                }
            }

            AddRange(game.Constructions);
            AddRange(game.ConstructionsForAdd);
            return list;
        }

        private static bool IsCellInOwnBuildZone(
            List<(Vector2Int Pos, Vector2Int Size, int Range)> providers, Vector2Int cell)
        {
            foreach (var (pos, size, range) in providers)
            {
                int gap = FootprintGapCells(cell, new Vector2Int(1, 1), pos, size);
                if (gap <= range)
                    return true;
            }

            return false;
        }

        private static bool IsCellInOwnBuildZone(Game game, Player player, Vector2Int cell) =>
            IsCellInOwnBuildZone(CollectBuildZoneProviders(game, player), cell);

        private static int FootprintGapCells(
            Vector2Int aPos, Vector2Int aSize, Vector2Int bPos, Vector2Int bSize)
        {
            int gapX = 0;
            if (aPos.X + aSize.X <= bPos.X) gapX = bPos.X - (aPos.X + aSize.X);
            else if (bPos.X + bSize.X <= aPos.X) gapX = aPos.X - (bPos.X + bSize.X);

            int gapY = 0;
            if (aPos.Y + aSize.Y <= bPos.Y) gapY = bPos.Y - (aPos.Y + aSize.Y);
            else if (bPos.Y + bSize.Y <= aPos.Y) gapY = aPos.Y - (bPos.Y + bSize.Y);

            return Math.Max(gapX, gapY);
        }

        private static bool IsLandCell(Game game, int x, int y)
        {
            if (game.Map == null || x < 0 || y < 0 || x >= game.Map.Width || y >= game.Map.Length)
                return false;
            var chunks = game.Map.GetArrayMap();
            return chunks[x, y] != null && chunks[x, y].Id != 0;
        }

        /// <summary>Мало свободной энергии — срочно нужна станция.</summary>
        private static bool NeedsPower(int freeEnergy) => freeEnergy < 30;

        private static int Count(Game game, Player player, string code) =>
            game.Constructions.Count(c =>
                c.OwnerId == player.Id && !c.IsDestroyed && c.Code == code)
            + game.ConstructionsForAdd.Count(c =>
                c.OwnerId == player.Id && !c.IsDestroyed && c.Code == code);

        /// <summary>
        /// В своей зоне стройки есть руда, которую ещё не накрывает ни один SupplyCenter.
        /// Нужно после AirDrop на остров: иначе бот сразу ставит краны.
        /// </summary>
        private static bool NeedsSupplyOnUnminedOre(Game game, Player player)
        {
            if (game.Map?.Ore == null)
                return false;

            var providers = CollectBuildZoneProviders(game, player);
            if (providers.Count == 0)
                return false;

            List<(Vector2Int Pos, Vector2Int Size)> supplies = new();
            foreach (Construction c in game.Constructions.Concat(game.ConstructionsForAdd))
            {
                if (c.OwnerId != player.Id || c.IsDestroyed || c.Code != SupplyCenterConstruction.Code)
                    continue;
                supplies.Add((c.Position.ToInt(), c.Size));
            }

            foreach (var (ox, oy, amount) in game.Map.Ore.Enumerate())
            {
                if (amount <= 0)
                    continue;
                Vector2Int oreCell = new(ox, oy);
                if (!IsCellInOwnBuildZone(providers, oreCell))
                    continue;
                if (IsOreCoveredBySupply(oreCell, supplies))
                    continue;
                return true;
            }

            return false;
        }

        private static bool IsOreCoveredBySupply(
            Vector2Int oreCell, List<(Vector2Int Pos, Vector2Int Size)> supplies)
        {
            int r = SupplyCenterConstruction.MineRadiusCells;
            foreach (var (pos, size) in supplies)
            {
                int minX = pos.X - r;
                int minY = pos.Y - r;
                int maxX = pos.X + size.X - 1 + r;
                int maxY = pos.Y + size.Y - 1 + r;
                if (oreCell.X >= minX && oreCell.X <= maxX && oreCell.Y >= minY && oreCell.Y <= maxY)
                    return true;
            }

            return false;
        }

        /// <summary>Площадка около провайдера зоны (МСЦ) / HQ. Для SupplyCenter — ещё и на руде.</summary>
        private static bool TryFindDenseBuildSite(Game game, Player player, string code, out Vector2Int site)
        {
            site = default;
            Construction? anchor = game.Constructions.FirstOrDefault(c =>
                c.OwnerId == player.Id && !c.IsDestroyed && Game.IsBuildZoneProvider(c.Code))
                ?? FindHq(game, player);
            if (anchor == null)
                return false;

            Vector2Int origin = anchor.Position.ToInt();
            Vector2Int size = ConstructionFactory.GetSize(code);
            int sx = Math.Max(1, size.X);
            int sy = Math.Max(1, size.Y);
            int zoneRange = Game.GetBuildZoneRange(anchor.Code);
            if (zoneRange <= 0)
                zoneRange = Game.BuildRangeCells;

            // Центр снабжения: сначала непокрытая руда в любой своей зоне (острова!), потом около якоря.
            if (code == SupplyCenterConstruction.Code)
            {
                if (TryFindSupplyOnUnminedOreSite(game, player, sx, sy, out site))
                    return true;
                if (TryFindSupplyOnOre(game, player, origin, zoneRange, sx, sy, out site))
                    return true;
            }

            Vector2Int? best = null;
            double bestScore = double.MaxValue;

            int rMax = Math.Min(SiteMaxRadius, zoneRange + Math.Max(sx, sy));
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
                        double score = distHq * 1.2 + nearest * 0.35;
                        if (score < bestScore)
                        {
                            bestScore = score;
                            best = candidate;
                        }
                    }
                }

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

        /// <summary>Supply 2×2 так, чтобы накрыть руду без добычи в своей зоне (в т.ч. у AirDropBase).</summary>
        private static bool TryFindSupplyOnUnminedOreSite(
            Game game, Player player, int sx, int sy, out Vector2Int site)
        {
            site = default;
            if (game.Map?.Ore == null)
                return false;

            var providers = CollectBuildZoneProviders(game, player);
            List<(Vector2Int Pos, Vector2Int Size)> supplies = new();
            foreach (Construction c in game.Constructions.Concat(game.ConstructionsForAdd))
            {
                if (c.OwnerId != player.Id || c.IsDestroyed || c.Code != SupplyCenterConstruction.Code)
                    continue;
                supplies.Add((c.Position.ToInt(), c.Size));
            }

            Vector2Int? best = null;
            double bestScore = double.MaxValue;

            foreach (var (ox, oy, amount) in game.Map.Ore.Enumerate())
            {
                if (amount <= 0)
                    continue;
                Vector2Int oreCell = new(ox, oy);
                if (!IsCellInOwnBuildZone(providers, oreCell))
                    continue;
                if (IsOreCoveredBySupply(oreCell, supplies))
                    continue;

                for (int oxOff = 0; oxOff < sx; oxOff++)
                {
                    for (int oyOff = 0; oyOff < sy; oyOff++)
                    {
                        Vector2Int candidate = new(ox - oxOff, oy - oyOff);
                        if (!game.CanPlaceConstruction(
                                SupplyCenterConstruction.Code, candidate, player.Id, out _))
                            continue;

                        double nearest = NearestOwnBuildingDistance(game, player, candidate, sx, sy);
                        double score = nearest;
                        if (score < bestScore)
                        {
                            bestScore = score;
                            best = candidate;
                        }
                    }
                }
            }

            if (!best.HasValue)
                return false;
            site = best.Value;
            return true;
        }

        private static bool TryFindSupplyOnOre(
            Game game, Player player, Vector2Int origin, int zoneRange,
            int sx, int sy, out Vector2Int site)
        {
            site = default;
            if (game.Map?.Ore == null)
                return false;

            Vector2Int? best = null;
            double bestScore = double.MaxValue;

            foreach (var (ox, oy, amount) in game.Map.Ore.Enumerate())
            {
                if (amount <= 0)
                    continue;

                // Кандидаты: origin footprint так, чтобы (ox,oy) попала внутрь 2×2.
                for (int oxOff = 0; oxOff < sx; oxOff++)
                {
                    for (int oyOff = 0; oyOff < sy; oyOff++)
                    {
                        Vector2Int candidate = new(ox - oxOff, oy - oyOff);
                        int dist = Math.Max(
                            Math.Abs(candidate.X - origin.X),
                            Math.Abs(candidate.Y - origin.Y));
                        if (dist > zoneRange + 4)
                            continue;
                        if (!game.CanPlaceConstruction(
                                SupplyCenterConstruction.Code, candidate, player.Id, out _))
                            continue;

                        double nearest = NearestOwnBuildingDistance(game, player, candidate, sx, sy);
                        double score = dist * 1.1 + nearest * 0.3;
                        if (score < bestScore)
                        {
                            bestScore = score;
                            best = candidate;
                        }
                    }
                }
            }

            if (!best.HasValue)
                return false;
            site = best.Value;
            return true;
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

            int expandReserve = GetExpandReserve(game, player);
            int bank = state.Resources;
            // Меньше 1k — только МСЦ (EnsureMobileHq) и транспортник с таймера экспанда.
            bool armyFrozen = bank < MinArmyBank;

            foreach (Construction c in game.Constructions)
            {
                if (c.OwnerId != player.Id || c.IsDestroyed || !c.IsBuilt || !c.IsPowered)
                    continue;
                if (c is not IUnitFactory factory)
                    continue;

                if (armyFrozen && c is not HeadquartersConstruction)
                    continue;

                // Солдаты — низкий приоритет: копим на экспанд.
                if (c is BarracksConstruction && expandReserve > 0)
                    continue;

                for (int n = 0; n < UnitProductionLimits.MaxOrders; n++)
                {
                    if (CountFactoryOrders(c) >= UnitProductionLimits.MaxOrders)
                        break;

                    string? unitCode = c switch
                    {
                        HeadquartersConstruction => null, // МСЦ заказывает EnsureMobileHq
                        BarracksConstruction => PickRandomFromPool(player, InfantryPool, BarracksConstruction.TryGetUnitRecipe),
                        MilitaryFactoryConstruction => PickRandomFromPool(player, VehiclePool, MilitaryFactoryConstruction.TryGetUnitRecipe),
                        AirFactoryConstruction => PickAirFactoryUnit(game, player),
                        WharfConstruction => "Ship",
                        _ => null
                    };
                    if (unitCode == null)
                        break;
                    if (!factory.TryGetRecipe(unitCode, out int cost, out _))
                        break;

                    int need = cost + expandReserve;
                    if (player.GetState().Resources < need)
                        break;

                    int before = player.GetState().Resources;
                    game.TryProduceUnit(c.Id, unitCode, player.Id);
                    if (player.GetState().Resources >= before)
                        break; // не приняли заказ — стоп по этому заводу
                }
            }
        }

        private static int CountFactoryOrders(Construction c)
        {
            if (c is not IUnitProductionHost host)
                return 0;
            int n = string.IsNullOrEmpty(host.CurrentProductionCode) ? 0 : 1;
            if (string.IsNullOrEmpty(host.ProductionQueueCsv))
                return n;
            foreach (string _ in host.ProductionQueueCsv.Split(
                         ',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                n++;
            return n;
        }

        private delegate bool TryRecipe(string unitCode, out int cost, out float buildSeconds);

        /// <summary>Боевая авиация; транспортник заказывает отдельный таймер экспанда.</summary>
        private string? PickAirFactoryUnit(Game game, Player player) =>
            PickRandomFromPool(player, ["AirUnit", "Helicopter"], AirFactoryConstruction.TryGetUnitRecipe);

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

        private static bool IsCombatUnit(Unit u) =>
            u.Code is not ("MobileConstructionCenter" or "AirTransporter");

        private static void TryAttack(Game game, Player player)
        {
            List<Unit> army = game.Units
                .Where(u => u.OwnerId == player.Id && !u.IsDestroyed && IsCombatUnit(u))
                .ToList();
            if (army.Count < AttackArmyThreshold)
                return;

            // Не дёргаем тех, кто уже идёт attack-move — иначе путь сбрасывается каждые N секунд.
            List<Unit> available = army
                .Where(u => !u.IsAttackMoving)
                .ToList();
            if (available.Count < 3)
                return;

            IAttackTarget? target = FindNearestEnemyTarget(game, player);
            if (target == null)
                return;

            Vector2Int dest = target.AimPosition.ToInt();
            // Почти всех свободных — мелкие волны тоже ок.
            int waveSize = Math.Max(3, available.Count * 3 / 4);
            waveSize = Math.Min(available.Count, waveSize);
            List<Unit> wave = available.Take(waveSize).ToList();
            if (game.Map != null)
                GroupMoveAssigner.IssueMove(wave, dest, attackMove: true, game.Map);

            foreach (Unit u in wave)
                u.SetAttackTarget(target);
        }

        private static IAttackTarget? FindNearestEnemyTarget(Game game, Player player)
        {
            Vector2Float origin = GetOwnOrigin(game, player);

            // 1) Штаб врага — всегда (бот «знает» базу, иначе без разведки никогда не атакует).
            IAttackTarget? bestHq = null;
            double bestHqDist = double.MaxValue;
            foreach (Construction c in game.Constructions)
            {
                if (c.OwnerId == player.Id || c.IsDestroyed || c.Code != "Headquarters")
                    continue;
                double dist = Vector2Float.Distance(origin, c.AimPosition);
                if (dist >= bestHqDist)
                    continue;
                bestHqDist = dist;
                bestHq = c;
            }

            // 2) Ближайший видимый юнит/здание — если ближе штаба, бить его.
            IAttackTarget? bestVisible = null;
            double bestVisibleDist = double.MaxValue;

            foreach (Construction c in game.Constructions)
            {
                if (c.OwnerId == player.Id || c.IsDestroyed)
                    continue;
                if (!game.CanSee(player.Id, c.AimPosition))
                    continue;
                double dist = Vector2Float.Distance(origin, c.AimPosition);
                if (dist >= bestVisibleDist)
                    continue;
                bestVisibleDist = dist;
                bestVisible = c;
            }

            foreach (Unit u in game.Units)
            {
                if (u.OwnerId == player.Id || u.IsDestroyed)
                    continue;
                if (!game.CanSee(player.Id, u.AimPosition))
                    continue;
                double dist = Vector2Float.Distance(origin, u.AimPosition);
                if (dist >= bestVisibleDist)
                    continue;
                bestVisibleDist = dist;
                bestVisible = u;
            }

            if (bestVisible != null && (bestHq == null || bestVisibleDist <= bestHqDist * 0.7))
                return bestVisible;

            return bestHq ?? bestVisible;
        }

        private static Vector2Float GetOwnOrigin(Game game, Player player)
        {
            Construction? hq = FindHq(game, player);
            if (hq != null)
                return hq.AimPosition;

            Unit? any = game.Units.FirstOrDefault(u => u.OwnerId == player.Id && !u.IsDestroyed);
            if (any != null)
                return any.Position;

            return Vector2Float.Zero;
        }
    }
}
