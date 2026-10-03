using RtsServer.App.Battle.Dto;
using RtsServer.App.Battle.Interfaces;

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

        public bool IsBuilt => BuildProgress >= 1f;

        /// <summary>Есть ли энергия. Провайдеры без потребления всегда true; потребители — по лимиту игрока.</summary>
        public bool IsPowered { get; set; } = true;

        public Vector2Float AimPosition => new(
            Position.X + Size.X * 0.5f,
            Position.Y + Size.Y * 0.5f);

        public virtual double GetBodyHeight() => 1.2;

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
            BuildDurationSeconds = Math.Max(0.1f, durationSeconds);
            BuildProgress = 0f;
        }

        public override void Update()
        {
            if (IsBuilt || BuildDurationSeconds <= 0f || Game == null)
                return;

            double dt = Game.TimeSystem.GetDelta();
            BuildProgress = Math.Min(1f, BuildProgress + (float)(dt / BuildDurationSeconds));
        }
    }
}
