using RtsServer.App.Battle.Dto;
using RtsServer.App.Battle.Interfaces;
using RtsServer.App.Battle.MapBattle.ChunksType;
using RtsServer.App.Battle.Units;

namespace RtsServer.App.Battle.Constructions
{
    /// <summary>Авиазавод — производство авиации. Самолётам нужна отдельная LandingPad.</summary>
    public class AirFactoryConstruction : Construction, IEnergyConsumer, IUnitProductionHost
    {
        public const int sizeX = 3;
        public const int sizeY = 3;
        public const int maxHealth = 4500;
        public const string Code = "AirFactory";
        public const int BuildCost = 280;
        public const float BuildTime = 12f;
        private const int RallySearchRadius = 16;

        public int EnergyRequired => IsBuilt ? 50 : 0;

        public float ProductionProgress =>
            _currentUnitCode == null ? 0f : Math.Clamp(_productionProgress, 0f, 1f);

        public string? CurrentProductionCode => _currentUnitCode;
        public IReadOnlyCollection<string> ProductionQueue => _queue;
        public string ProductionQueueCsv => _queueCsv;
        public int OrdersCount =>
            (_currentUnitCode != null ? 1 : 0) + _queue.Count;

        /// <summary>Сколько AirUnit сейчас в производстве/очереди этого завода.</summary>
        public int CountAirUnitsInProduction
        {
            get
            {
                int n = 0;
                if (_currentUnitCode == "AirUnit")
                    n++;
                foreach (string code in _queue)
                {
                    if (code == "AirUnit")
                        n++;
                }
                return n;
            }
        }

        private string? _currentUnitCode;
        private float _productionProgress;
        private float _productionDuration = 1f;
        private readonly Queue<string> _queue = new();
        private string _queueCsv = "";

        public AirFactoryConstruction(Vector2Int position, int playerOwner)
            : base(maxHealth, maxHealth, position, new(sizeX, sizeY), Code, playerOwner)
        {
        }

        public static bool TryGetUnitRecipe(string unitCode, out int cost, out float buildSeconds)
        {
            switch (unitCode)
            {
                case "AirUnit":
                    cost = 250;
                    buildSeconds = 14f;
                    return true;
                case "Helicopter":
                    cost = 350;
                    buildSeconds = 14f;
                    return true;
                case "AirTransporter":
                    cost = 320;
                    buildSeconds = 16f;
                    return true;
                default:
                    cost = 0;
                    buildSeconds = 0f;
                    return false;
            }
        }

        public bool CanProduce(string unitCode) => TryGetUnitRecipe(unitCode, out _, out _);

        public bool TryGetRecipe(string unitCode, out int cost, out float buildSeconds) =>
            TryGetUnitRecipe(unitCode, out cost, out buildSeconds);

        public bool Enqueue(string unitCode, float buildSeconds)
        {
            if (!IsBuilt || !CanProduce(unitCode)) return false;
            if (OrdersCount >= UnitProductionLimits.MaxOrders) return false;

            // Самолёту нужна свободная посадочная площадка у владельца.
            if (unitCode == "AirUnit"
                && !LandingPadConstruction.OwnerCanAcceptNewAirUnit(Game, OwnerId))
                return false;

            if (_currentUnitCode == null)
            {
                _currentUnitCode = unitCode;
                _productionDuration = Math.Max(0.1f, buildSeconds);
                _productionProgress = 0f;
            }
            else
            {
                _queue.Enqueue(unitCode);
                RebuildQueueCsv();
            }

            return true;
        }

        public bool TryCancelOne(string unitCode) =>
            UnitProductionQueueOps.TryCancelOne(
                ref _currentUnitCode,
                ref _productionProgress,
                ref _productionDuration,
                _queue,
                RebuildQueueCsv,
                code => TryGetUnitRecipe(code, out _, out float seconds) ? (true, seconds) : (false, 0f),
                unitCode);

        private void RebuildQueueCsv()
        {
            _queueCsv = _queue.Count == 0 ? "" : string.Join(",", _queue);
        }

        public override void Update()
        {
            base.Update();
            if (!IsBuilt || !IsPowered || Game == null || _currentUnitCode == null)
                return;

            double dt = Game.TimeSystem.GetDelta();
            _productionProgress += (float)(dt / _productionDuration);
            if (_productionProgress < 1f) return;

            SpawnUnit(_currentUnitCode);
            _currentUnitCode = null;
            _productionProgress = 0f;

            if (_queue.Count == 0) return;

            string next = _queue.Dequeue();
            RebuildQueueCsv();
            if (!TryGetUnitRecipe(next, out _, out float seconds))
                return;

            _currentUnitCode = next;
            _productionDuration = Math.Max(0.1f, seconds);
            _productionProgress = 0f;
        }

        private void SpawnUnit(string unitCode)
        {
            if (Game?.Map == null) return;

            Vector2Int origin = Position.ToInt();
            int gateX = origin.X + Size.X / 2;
            int gateY = Math.Max(0, origin.Y - 1);
            Vector2Int gate = new(gateX, gateY);
            Vector2Int rally = FindFreeRallyCell() ?? gate;

            try
            {
                if (unitCode == "AirUnit")
                {
                    LandingPadConstruction? pad = LandingPadConstruction.FindNearestWithFreeSlot(
                        Game, OwnerId, AimPosition);
                    if (pad == null)
                        return;

                    Unit air = UnitFactory.GetByCode(unitCode, pad.AimPosition, OwnerId);
                    air.SetGame(Game);
                    if (air is not AirUnit airUnit
                        || !pad.LandingPads.TryReserve(airUnit, out int slot, out Vector2Float padPos))
                        return;

                    // Слот резервируем, сам самолёт вылетает с завода и садится на площадку.
                    // PendingRally не ставим — иначе Init→SetTarget сорвёт Returning.
                    airUnit.BindLandingPad(pad, slot, padPos, startParked: false);
                    airUnit.StartDeliveryToPad(AimPosition);
                    Game.UnitsForAdd.Add(air);
                    return;
                }

                Unit unit = UnitFactory.GetByCode(unitCode, gate.GetFloat(), OwnerId);
                unit.SetGame(Game);
                unit.PendingRallyCell = rally;
                Game.UnitsForAdd.Add(unit);
            }
            catch (Exception)
            {
            }
        }

        private Vector2Int? FindFreeRallyCell()
        {
            if (Game?.Map == null) return null;

            Vector2Int origin = Position.ToInt();
            int centerX = origin.X + Size.X / 2;
            int exitY = origin.Y - 1;

            for (int row = 1; row <= RallySearchRadius; row++)
            {
                int y = exitY - (row - 1);
                if (y < 0) break;

                for (int dx = 0; dx <= row; dx++)
                {
                    if (IsCellFreeForRally(centerX + dx, y))
                        return new Vector2Int(centerX + dx, y);
                    if (dx > 0 && IsCellFreeForRally(centerX - dx, y))
                        return new Vector2Int(centerX - dx, y);
                }
            }

            return null;
        }

        private bool IsCellFreeForRally(int x, int y)
        {
            if (Game?.Map == null) return false;
            if (x < 0 || y < 0 || x >= Game.Map.Width || y >= Game.Map.Length)
                return false;

            ChunkBase chunk;
            try { chunk = Game.Map.GetChunk(x, y); }
            catch { return false; }

            if (chunk.Id == 0) return false;

            foreach (Construction c in Game.Constructions.Concat(Game.ConstructionsForAdd))
            {
                if (c.IsDestroyed) continue;
                Vector2Int origin = c.Position.ToInt();
                if (x >= origin.X && x < origin.X + c.Size.X
                    && y >= origin.Y && y < origin.Y + c.Size.Y)
                    return false;
            }

            if (chunk.UnitsInPoint != null)
            {
                foreach (Unit u in chunk.UnitsInPoint)
                {
                    if (u != null && !u.IsDestroyed)
                        return false;
                }
            }

            foreach (Unit u in Game.Units.Concat(Game.UnitsForAdd))
            {
                if (u.IsDestroyed) continue;
                if (u.PendingRallyCell is Vector2Int pending && pending.X == x && pending.Y == y)
                    return false;
                Vector2Int pos = u.Position.ToInt();
                if (pos.X == x && pos.Y == y)
                    return false;
            }

            return true;
        }
    }
}
