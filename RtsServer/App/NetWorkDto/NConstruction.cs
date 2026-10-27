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
        /// <summary>Выстрелы турели (hitscan): клиент рисует трассер.</summary>
        public int ShotSerial { get; set; }
        public float ShotAimX { get; set; }
        public float ShotAimY { get; set; }
        /// <summary>Угол огневой точки (Y), как у AttackPoint юнита.</summary>
        public float TurretRotation { get; set; }
        /// <summary>Поворот постройки по Y (градусы). Верфь смотрит лицом к воде.</summary>
        public float Rotation { get; set; }
        /// <summary>Сворачивание в мобильный юнит (BuildProgress идёт 1→0).</summary>
        public bool IsPacking { get; set; }

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
            float BuildDurationSeconds = 0f,
            int ShotSerial = 0,
            float ShotAimX = 0f,
            float ShotAimY = 0f,
            float TurretRotation = 0f,
            float Rotation = 0f,
            bool IsPacking = false)
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
            this.ShotSerial = ShotSerial;
            this.ShotAimX = ShotAimX;
            this.ShotAimY = ShotAimY;
            this.TurretRotation = TurretRotation;
            this.Rotation = Rotation;
            this.IsPacking = IsPacking;
        }
    }
}
