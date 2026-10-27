using RtsServer.App.Battle.Dto;
using RtsServer.App.Battle.Interfaces;
using RtsServer.App.Battle.MapBattle;

namespace RtsServer.App.Battle.Constructions
{
    /// <summary>
    /// Центр снабжения: склад + добыча руды из клеток в зоне footprint + MineRadius.
    /// </summary>
    public class SupplyCenterConstruction : Construction, IResourceProducer, IResourceStorage, IEnergyConsumer
    {
        public const int sizeX = 2;
        public const int sizeY = 2;
        public const int maxHealth = 4000;
        public const string Code = "SupplyCenter";
        public const int BuildCost = 120;
        public const float BuildTime = 7f;

        /// <summary>Клетки вокруг footprint, откуда берётся руда.</summary>
        public const int MineRadiusCells = 1;
        /// <summary>Макс. $/мин при наличии руды.</summary>
        public const float OrePerMinute = 120f;

        public int LimitResources => 3000;
        public int ResourcePerMinute => (int)OrePerMinute;
        public int EnergyRequired => IsBuilt ? 25 : 0;
        public float Resources { get; set; }

        /// <summary>Дробный накопитель: иначе Ceiling(want) снимал 1 руду с карты каждый тик (~1200/мин).</summary>
        private float _oreExtractCarry;

        public SupplyCenterConstruction(Vector2Int position, int playerOwner)
            : base(maxHealth, maxHealth, position, new(sizeX, sizeY), Code, playerOwner) { }

        public bool TrySpend(float amount)
        {
            if (!IsBuilt || Resources < amount) return false;
            Resources -= amount;
            return true;
        }

        public void AddResources(float amount)
        {
            if (!IsBuilt) return;
            Resources = Math.Min(Resources + amount, LimitResources);
        }

        public void ProduceResources(double deltaTime)
        {
            if (!IsBuilt || !IsPowered || Game?.Map?.Ore == null || deltaTime <= 0)
                return;

            float room = LimitResources - Resources;
            if (room <= 0.01f)
                return;

            float want = (float)(OrePerMinute * deltaTime / 60.0);
            want = Math.Min(want, room);
            if (want < 0.001f)
                return;

            _oreExtractCarry += want;
            int need = (int)_oreExtractCarry;
            if (need < 1)
                return;

            int extracted = ExtractFromZone(need);
            if (extracted <= 0)
                return;

            _oreExtractCarry -= extracted;
            if (_oreExtractCarry < 0f)
                _oreExtractCarry = 0f;

            AddResources(extracted);
        }

        private int ExtractFromZone(int need)
        {
            OreDepositMap ore = Game!.Map!.Ore;
            Vector2Int origin = Position.ToInt();
            int minX = origin.X - MineRadiusCells;
            int minY = origin.Y - MineRadiusCells;
            int maxX = origin.X + Size.X - 1 + MineRadiusCells;
            int maxY = origin.Y + Size.Y - 1 + MineRadiusCells;

            int left = need;
            int taken = 0;

            // Несколько проходов — равномернее высасываем зону.
            for (int pass = 0; pass < 3 && left > 0; pass++)
            {
                for (int x = minX; x <= maxX && left > 0; x++)
                {
                    for (int y = minY; y <= maxY && left > 0; y++)
                    {
                        if (x < 0 || y < 0 || x >= Game.Map.Width || y >= Game.Map.Length)
                            continue;
                        int got = ore.Extract(x, y, Math.Min(left, 8));
                        taken += got;
                        left -= got;
                    }
                }
            }

            return taken;
        }

        public override void Update()
        {
            base.Update();
            if (Game == null) return;
            ProduceResources(Game.TimeSystem.GetDelta());
        }

        public override double GetBodyHeight() => GetGroundElevation() + 1.1;
    }
}
