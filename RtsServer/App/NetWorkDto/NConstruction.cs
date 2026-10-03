using RtsServer.App.Battle.Dto;

namespace RtsServer.App.NetWorkDto
{
    public struct NConstruction
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public float Health { get; set; }
        public float MaxHealth { get; set; }
        public Vector2Int Position { get; set; }
        public float BuildProgress { get; set; }
        /// <summary>Полная длительность текущего строительства (сек), 0 если уже готово.</summary>
        public float BuildDurationSeconds { get; set; }
        public int OwnerId { get; set; }
        public bool IsPowered { get; set; }
        /// <summary>0..1 прогресс текущего производства (завод).</summary>
        public float ProductionProgress { get; set; }
        /// <summary>Код текущего юнита в производстве, иначе пусто.</summary>
        public string ProductionUnitCode { get; set; }
        /// <summary>Очередь после текущего, через запятую (Baggy,TankT1).</summary>
        public string ProductionQueue { get; set; }

        public NConstruction(
            int Id,
            string Code,
            float Health,
            Vector2Int Position,
            float BuildProgress = 1f,
            int OwnerId = 0,
            float MaxHealth = 0f,
            bool IsPowered = true,
            float ProductionProgress = 0f,
            string ProductionUnitCode = "",
            string ProductionQueue = "",
            float BuildDurationSeconds = 0f)
        {
            this.Id = Id;
            this.Code = Code;
            this.Health = Health;
            this.Position = Position;
            this.BuildProgress = BuildProgress;
            this.OwnerId = OwnerId;
            this.MaxHealth = MaxHealth > 0f ? MaxHealth : Health;
            this.IsPowered = IsPowered;
            this.ProductionProgress = ProductionProgress;
            this.ProductionUnitCode = ProductionUnitCode ?? "";
            this.ProductionQueue = ProductionQueue ?? "";
            this.BuildDurationSeconds = BuildDurationSeconds;
        }
    }
}
