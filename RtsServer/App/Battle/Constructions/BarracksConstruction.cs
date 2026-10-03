using RtsServer.App.Battle.Dto;
using RtsServer.App.Battle.Interfaces;
using RtsServer.App.Battle.MapBattle.ChunksType;
using RtsServer.App.Battle.Units;

namespace RtsServer.App.Battle.Constructions
{
    /// <summary>Барак — производство пехоты: Soldier / Grenadier.</summary>
    public class BarracksConstruction : Construction, IEnergyConsumer, IUnitFactory
    {
        public const int sizeX = 2;
        public const int sizeY = 2;
        public const int maxHealth = 3500;
        public const string Code = "Barracks";
        public const int BuildCost = 120;
        public const float BuildTime = 7f;
        private const int RallySearchRadius = 16;

        public int EnergyRequired => IsBuilt ? 25 : 0;

        public float ProductionProgress =>
            _currentUnitCode == null ? 0f : Math.Clamp(_productionProgress, 0f, 1f);

        public string? CurrentProductionCode => _currentUnitCode;
        public IReadOnlyCollection<string> ProductionQueue => _queue;

        private string? _currentUnitCode;
        private float _productionProgress;
        private float _productionDuration = 1f;
        private readonly Queue<string> _queue = new();

        public BarracksConstruction(Vector2Int position, int playerOwner)
            : base(maxHealth, maxHealth, position, new(sizeX, sizeY), Code, playerOwner) { }

        public static bool TryGetUnitRecipe(string unitCode, out int cost, out float buildSeconds)
        {
            switch (unitCode)
            {
                case "Soldier":
                    cost = 40;
                    buildSeconds = 4f;
                    return true;
                case "Grenadier":
                    cost = 70;
                    buildSeconds = 6f;
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

            if (_currentUnitCode == null)
            {
                _currentUnitCode = unitCode;
                _productionDuration = Math.Max(0.1f, buildSeconds);
                _productionProgress = 0f;
            }
            else
            {
                _queue.Enqueue(unitCode);
            }

            return true;
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
                Vector2Int o = c.Position.ToInt();
                if (x >= o.X && x < o.X + c.Size.X && y >= o.Y && y < o.Y + c.Size.Y)
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
