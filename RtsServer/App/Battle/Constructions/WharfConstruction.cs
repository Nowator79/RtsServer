using RtsServer.App.Battle.Dto;
using RtsServer.App.Battle.Interfaces;
using RtsServer.App.Battle.MapBattle.ChunksType;
using RtsServer.App.Battle.Units;

namespace RtsServer.App.Battle.Constructions
{
    /// <summary>Верфь — производство кораблей на соседней воде.</summary>
    public class WharfConstruction : Construction, IEnergyConsumer, IUnitProductionHost
    {
        public const int sizeX = 3;
        public const int sizeY = 3;
        public const int maxHealth = 4800;
        public const string Code = "Wharf";
        public const int BuildCost = 260;
        public const float BuildTime = 11f;
        private const int RallySearchRadius = 20;

        public int EnergyRequired => IsBuilt ? 40 : 0;

        public float ProductionProgress =>
            _currentUnitCode == null ? 0f : Math.Clamp(_productionProgress, 0f, 1f);

        public string? CurrentProductionCode => _currentUnitCode;
        public IReadOnlyCollection<string> ProductionQueue => _queue;
        public string ProductionQueueCsv => _queueCsv;
        public int OrdersCount =>
            (_currentUnitCode != null ? 1 : 0) + _queue.Count;

        private string? _currentUnitCode;
        private float _productionProgress;
        private float _productionDuration = 1f;
        private readonly Queue<string> _queue = new();
        private string _queueCsv = "";

        public WharfConstruction(Vector2Int position, int playerOwner)
            : base(maxHealth, maxHealth, position, new(sizeX, sizeY), Code, playerOwner) { }

        // Сторона, обращённая к воде. Карта статична, поэтому считаем один раз.
        private const int FacingProbeDepth = 4;
        private bool _facingResolved;
        private float _facingYaw;
        private int _facingDirX;
        private int _facingDirY = -1;

        /// <summary>Лицом к воде: клиент ставит модель этим углом.</summary>
        public override float FacingYaw
        {
            get
            {
                EnsureFacing();
                return _facingYaw;
            }
        }

        private void EnsureFacing()
        {
            if (_facingResolved || Game?.Map == null)
                return;

            var chunks = Game.Map.GetArrayMap();
            Vector2Int origin = Position.ToInt();

            // Порядок = приоритет при равенстве: вниз (прежнее поведение), вправо, вверх, влево.
            int[,] dirs = { { 0, -1 }, { 1, 0 }, { 0, 1 }, { -1, 0 } };
            int bestScore = 0;
            int bestDirX = 0;
            int bestDirY = -1;

            for (int d = 0; d < 4; d++)
            {
                int dx = dirs[d, 0];
                int dy = dirs[d, 1];
                int score = 0;

                for (int lane = 0; lane < (dy != 0 ? Size.X : Size.Y); lane++)
                {
                    for (int depth = 1; depth <= FacingProbeDepth; depth++)
                    {
                        int x;
                        int y;
                        if (dy != 0)
                        {
                            x = origin.X + lane;
                            y = dy < 0 ? origin.Y - depth : origin.Y + Size.Y - 1 + depth;
                        }
                        else
                        {
                            x = dx < 0 ? origin.X - depth : origin.X + Size.X - 1 + depth;
                            y = origin.Y + lane;
                        }

                        if (x < 0 || y < 0 || x >= Game.Map.Width || y >= Game.Map.Length)
                            continue;
                        if (chunks[x, y] != null && chunks[x, y].Id == 0)
                            score += FacingProbeDepth + 1 - depth; // ближняя вода весит больше
                    }
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestDirX = dx;
                    bestDirY = dy;
                }
            }

            if (bestScore == 0 && TryFindNearestWaterDirection(chunks, origin, out int fx, out int fy))
            {
                // Вода только по диагонали — смотрим на ближайшую клетку воды.
                _facingYaw = (float)(Math.Atan2(-fx, -fy) * 180.0 / Math.PI);
                _facingDirX = Math.Sign(fx);
                _facingDirY = Math.Sign(fy);
                _facingResolved = true;
                return;
            }

            // Модель при yaw=0 смотрит в -Y; yaw поворачивает этот вектор в сторону воды.
            _facingYaw = (float)(Math.Atan2(-bestDirX, -bestDirY) * 180.0 / Math.PI);
            _facingDirX = bestDirX;
            _facingDirY = bestDirY;
            _facingResolved = true;
        }

        private bool TryFindNearestWaterDirection(
            ChunkBase[,] chunks, Vector2Int origin, out int dirX, out int dirY)
        {
            dirX = 0;
            dirY = -1;
            double cx = origin.X + Size.X * 0.5;
            double cy = origin.Y + Size.Y * 0.5;
            double bestDist = double.MaxValue;
            bool found = false;

            for (int x = origin.X - RallySearchRadius; x <= origin.X + Size.X + RallySearchRadius; x++)
            {
                for (int y = origin.Y - RallySearchRadius; y <= origin.Y + Size.Y + RallySearchRadius; y++)
                {
                    if (x < 0 || y < 0 || x >= Game.Map.Width || y >= Game.Map.Length)
                        continue;
                    if (chunks[x, y] == null || chunks[x, y].Id != 0)
                        continue;

                    double ddx = x + 0.5 - cx;
                    double ddy = y + 0.5 - cy;
                    double dist = ddx * ddx + ddy * ddy;
                    if (dist >= bestDist) continue;

                    bestDist = dist;
                    dirX = (int)Math.Round(ddx);
                    dirY = (int)Math.Round(ddy);
                    found = true;
                }
            }

            return found && (dirX != 0 || dirY != 0);
        }

        public static bool TryGetUnitRecipe(string unitCode, out int cost, out float buildSeconds)
        {
            switch (unitCode)
            {
                case "Ship":
                    cost = 380;
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

            Vector2Int? water = FindFreeWaterCell();
            if (water == null)
                return;

            Vector2Int spawn = water.Value;
            Vector2Int rally = FindFreeWaterCell(preferNear: spawn) ?? spawn;

            try
            {
                Unit unit = UnitFactory.GetByCode(unitCode, spawn.GetFloat(), OwnerId);
                unit.SetGame(Game);
                unit.PendingRallyCell = rally;
                Game.UnitsForAdd.Add(unit);
            }
            catch (Exception)
            {
            }
        }

        private Vector2Int? FindFreeWaterCell(Vector2Int? preferNear = null)
        {
            if (Game?.Map == null) return null;

            Vector2Int origin = Position.ToInt();
            int centerX = origin.X + Size.X / 2;
            int centerY = origin.Y + Size.Y / 2;
            if (preferNear.HasValue)
            {
                centerX = preferNear.Value.X;
                centerY = preferNear.Value.Y;
            }
            else
            {
                // Корабли выходят с «лицевой» стороны верфи.
                EnsureFacing();
                centerX += _facingDirX * (Size.X / 2 + 1);
                centerY += _facingDirY * (Size.Y / 2 + 1);
            }

            for (int radius = 1; radius <= RallySearchRadius; radius++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != radius)
                            continue;
                        if (IsWaterCellFree(centerX + dx, centerY + dy))
                            return new Vector2Int(centerX + dx, centerY + dy);
                    }
                }
            }

            return null;
        }

        private bool IsWaterCellFree(int x, int y)
        {
            if (Game?.Map == null) return false;
            if (x < 0 || y < 0 || x >= Game.Map.Width || y >= Game.Map.Length)
                return false;

            ChunkBase chunk;
            try { chunk = Game.Map.GetChunk(x, y); }
            catch { return false; }

            if (chunk.Id != 0) return false; // только вода

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
