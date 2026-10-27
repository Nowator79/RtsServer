using RtsServer.App.Battle.Dto;
using RtsServer.App.Battle.Interfaces;
using RtsServer.App.Battle.MapBattle;

namespace RtsServer.App.Battle.Constructions
{
    public abstract class Construction : BattleEntity, IAttackTarget
    {
        public Health Health { get; protected set; }
        public Vector2Int Size { get; protected set; }

        /// <summary>0..1. Картовые постройки стартуют готовыми (1).</summary>
        public float BuildProgress { get; protected set; } = 1f;

        /// <summary>Длительность текущего строительства в секундах. 0 — уже построено.</summary>
        public float BuildDurationSeconds { get; protected set; }

        /// <summary>Идёт сворачивание в мобильный юнит (прогресс 1→0).</summary>
        public bool IsPacking { get; protected set; }

        public bool IsBuilt => !IsPacking && BuildProgress >= 1f;

        /// <summary>
        /// Поворот постройки по Y (градусы, как Quaternion.Euler(0, yaw, 0) в Unity).
        /// 0 — «лицом» к -Y карты (вниз). Верфь переопределяет и поворачивается к воде.
        /// </summary>
        public virtual float FacingYaw => 0f;

        /// <summary>Есть ли энергия. Провайдеры без потребления всегда true; потребители — по лимиту игрока.</summary>
        public bool IsPowered { get; set; } = true;

        public Vector2Float AimPosition => new(
            Position.X + Size.X * 0.5f,
            Position.Y + Size.Y * 0.5f);

        public virtual double GetGroundElevation() => TerrainHeights.SampleAt(Game, AimPosition);

        public virtual double GetBodyHeight() => GetGroundElevation() + 1.2;

        public AttackTargetDomain AttackDomain => AttackTargetDomain.Construction;
        /// <summary>Здания по умолчанию тяжёлые — пулемёты слабо, танки/AGM сильно.</summary>
        public virtual ArmorType Armor => ArmorType.Heavy;

        /// <summary>Клетка внутри габарита постройки (включая недостроенную) — непроходима для наземных.</summary>
        public bool OccupiesCell(int x, int y)
        {
            if (IsDestroyed) return false;
            Vector2Int origin = Position.ToInt();
            return x >= origin.X && x < origin.X + Size.X
                && y >= origin.Y && y < origin.Y + Size.Y;
        }

        public bool OccupiesCell(Vector2Int cell) => OccupiesCell(cell.X, cell.Y);

        public Construction(
            int health,
            int maxhealth,
            Vector2Int position,
            Vector2Int size,
            string code,
            int playerOwner
            ) : base(code, playerOwner, position)
        {
            Health = new(health, maxhealth, this);
            Size = size;
        }

        public void SetId(int Id)
        {
            this.Id = Id;
        }

        /// <summary>Запустить процесс строительства (после оплаты).</summary>
        public void BeginConstruction(float durationSeconds)
        {
            IsPacking = false;
            BuildDurationSeconds = Math.Max(0.1f, durationSeconds);
            BuildProgress = 0f;
        }

        /// <summary>Сворачивание: BuildProgress 1→0 за durationSeconds, затем OnPackingComplete.</summary>
        public bool BeginPacking(float durationSeconds)
        {
            if (IsDestroyed || IsPacking || BuildProgress < 1f)
                return false;

            IsPacking = true;
            BuildDurationSeconds = Math.Max(0.1f, durationSeconds);
            // Чуть ниже 1 — сразу !IsBuilt (энергия/производство стоп).
            BuildProgress = 0.999f;
            return true;
        }

        public override void Update()
        {
            if (Game == null || BuildDurationSeconds <= 0f)
                return;

            double dt = Game.TimeSystem.GetDelta();

            if (IsPacking)
            {
                BuildProgress = Math.Max(0f, BuildProgress - (float)(dt / BuildDurationSeconds));
                if (BuildProgress <= 0.001f)
                {
                    BuildProgress = 0f;
                    IsPacking = false;
                    BuildDurationSeconds = 0f;
                    OnPackingComplete();
                }
                return;
            }

            if (BuildProgress >= 1f)
                return;

            BuildProgress = Math.Min(1f, BuildProgress + (float)(dt / BuildDurationSeconds));
        }

        /// <summary>Конец сворачивания — подклассы спавнят юнит и Destroy.</summary>
        protected virtual void OnPackingComplete()
        {
        }
    }
}
