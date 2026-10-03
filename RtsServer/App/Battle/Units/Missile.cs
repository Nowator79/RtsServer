using RtsServer.App.Battle.Constructions;
using RtsServer.App.Battle.Dto;
using RtsServer.App.Battle.Explosion;
using RtsServer.App.Battle.Interfaces;
using RtsServer.App.Battle.Tools;
using RtsServer.App.Battle.Units.AttackingPoint;
using RtsServer.App.Tools;

namespace RtsServer.App.Battle.Units
{
    /// <summary>
    /// Снаряд: баллистика или самонаведение (AGM) с лимитом поворота и падением после топлива.
    /// Position — проекция на карту (X/Y), Height — высота (Z).
    /// </summary>
    public class Missile : BattleEntity
    {
        public int Id { get; private set; }
        public string Code { get; }
        public float Speed { get; private set; }
        public BaseAttackingPoint? CurrentAttackPoint { get; private set; }
        public float Range { get; private set; }
        public float Damage { get; private set; }

        /// <summary>Высота над картой.</summary>
        public double Height { get; private set; }

        /// <summary>Скорость: X/Y карта, Z высота.</summary>
        public Vector3Float Velocity { get; private set; }

        public Vector2Float TargetMap { get; private set; }
        public double TargetHeight { get; private set; }

        public new Game Game => _game;

        private readonly Game _game;
        private readonly TimeSystem _timeSystem;
        private readonly IAttackTarget? _lockTarget;
        private readonly bool _isGuided;
        private readonly bool _applyGravity;
        private readonly int? _shooterUnitId;
        private readonly ArmorDamageProfile _armorDamage;
        private double _age;
        private Vector2Float _prevPosition;
        private double _prevHeight;

        private const double GroundLevel = 0.05;
        private const double HitRadiusUnit = 1.1;
        private const double HitHeightSlop = 1.6;
        /// <summary>Быстрые трассеры (mg) — чуть шире радиус, иначе проскакивают за тик.</summary>
        private const double HitRadiusFastTracer = 1.45;

        /// <summary>Сколько секунд ракета держит лок и летит под тягой.</summary>
        private const double HomingDurationSeconds = 2.0;
        /// <summary>Макс. угловая скорость доворота (град/с) — может не успеть изогнуться.</summary>
        private const double MaxTurnRateDegreesPerSec = 85.0;

        public void SetId(int id) => Id = id;

        public Missile(
            string code,
            Vector2Float startMap,
            double startHeight,
            Vector3Float velocity,
            Vector2Float targetMap,
            double targetHeight,
            float speed,
            float range,
            float damage,
            TimeSystem timeSystem,
            BaseAttackingPoint? currentAttackPoint,
            int playerOwner,
            Game game,
            IAttackTarget? lockTarget = null,
            bool guided = false,
            int? shooterUnitId = null,
            bool applyGravity = true,
            ArmorDamageProfile? armorDamage = null)
            : base(code, playerOwner, startMap)
        {
            Code = code;
            Position = startMap;
            Height = startHeight;
            Velocity = velocity;
            TargetMap = targetMap;
            TargetHeight = targetHeight;
            Speed = speed;
            IsDestroyed = false;
            _timeSystem = timeSystem;
            CurrentAttackPoint = currentAttackPoint;
            Range = range;
            Damage = damage;
            _lockTarget = lockTarget;
            _isGuided = guided || code is "agm_rocket" or "aam_rocket";
            _game = game ?? throw new ArgumentNullException(nameof(game));
            _shooterUnitId = shooterUnitId;
            _applyGravity = applyGravity && !_isGuided;
            _armorDamage = armorDamage ?? ArmorDamageProfile.Zero;
            _prevPosition = startMap;
            _prevHeight = startHeight;
        }

        public void Update()
        {
            if (IsDestroyed)
                return;

            float deltaTime = (float)_timeSystem.GetDelta();
            if (deltaTime <= 0)
                return;

            _age += deltaTime;
            _prevPosition = Position;
            _prevHeight = Height;

            if (_isGuided)
                UpdateGuided(deltaTime);
            else
                UpdateBallistic(deltaTime);

            if (TryHitTarget() || HasHitGround())
            {
                if (Height < GroundLevel)
                    Height = GroundLevel;
                Destroy();
            }
        }

        private void UpdateBallistic(float deltaTime)
        {
            if (_applyGravity)
            {
                Velocity = new Vector3Float(
                    Velocity.X,
                    Velocity.Y,
                    Velocity.Z - Ballistics.Gravity * deltaTime);
            }

            Integrate(deltaTime);
        }

        private void UpdateGuided(float deltaTime)
        {
            bool powered = _age < HomingDurationSeconds;
            IAttackTarget? live = ResolveLockTarget();

            if (powered && live != null)
            {
                Vector3Float desired = DesiredVelocityToward(live);
                Velocity = SteerVelocity(Velocity, desired, MaxTurnRateDegreesPerSec * deltaTime);

                // Под тягой держим скорость примерно постоянной.
                double mag = Velocity.Magnitude();
                if (mag > 1e-6)
                    Velocity = Velocity * (Speed / mag);
                else
                    Velocity = desired.Magnitude() > 1e-6 ? desired.Normalized() * Speed : Velocity;
            }
            else
            {
                // Топливо кончилось — лок теряется, падаем по текущей скорости + гравитация.
                Velocity = new Vector3Float(
                    Velocity.X,
                    Velocity.Y,
                    Velocity.Z - Ballistics.Gravity * deltaTime);
            }

            Integrate(deltaTime);
        }

        private IAttackTarget? ResolveLockTarget()
        {
            if (_lockTarget != null && !_lockTarget.IsDestroyed)
                return _lockTarget;
            IAttackTarget? fromGun = CurrentAttackPoint?.Target;
            if (fromGun != null && !fromGun.IsDestroyed)
                return fromGun;
            return null;
        }

        private Vector3Float DesiredVelocityToward(IAttackTarget target)
        {
            double tx = target.AimPosition.X;
            double ty = target.AimPosition.Y;
            double tz = target.GetBodyHeight();

            // Обновляем точку для фоллбек-хита / визуала.
            TargetMap = target.AimPosition;
            TargetHeight = tz;

            Vector3Float toTarget = new(
                tx - Position.X,
                ty - Position.Y,
                tz - Height);

            double mag = toTarget.Magnitude();
            if (mag < 1e-6)
                return Velocity.Magnitude() > 1e-6 ? Velocity.Normalized() * Speed : new Vector3Float(Speed, 0, 0);

            return toTarget.Normalized() * Speed;
        }

        /// <summary>Поворачиваем вектор скорости к desired не быстрее maxAngleDegrees за тик.</summary>
        private static Vector3Float SteerVelocity(Vector3Float current, Vector3Float desired, double maxAngleDegrees)
        {
            double curMag = current.Magnitude();
            Vector3Float curDir = curMag > 1e-6 ? current.Normalized() : desired.Normalized();
            Vector3Float desDir = desired.Magnitude() > 1e-6 ? desired.Normalized() : curDir;

            double dot = Math.Clamp(
                curDir.X * desDir.X + curDir.Y * desDir.Y + curDir.Z * desDir.Z,
                -1.0, 1.0);
            double angleDeg = Math.Acos(dot) * (180.0 / Math.PI);

            if (angleDeg <= 0.01 || maxAngleDegrees <= 0)
                return desDir * (curMag > 1e-6 ? curMag : desired.Magnitude());

            if (angleDeg <= maxAngleDegrees)
                return desDir * (curMag > 1e-6 ? curMag : desired.Magnitude());

            double t = maxAngleDegrees / angleDeg;
            Vector3Float blended = new(
                curDir.X + (desDir.X - curDir.X) * t,
                curDir.Y + (desDir.Y - curDir.Y) * t,
                curDir.Z + (desDir.Z - curDir.Z) * t);
            double bMag = blended.Magnitude();
            if (bMag < 1e-6)
                return curDir * curMag;
            return blended.Normalized() * (curMag > 1e-6 ? curMag : desired.Magnitude());
        }

        private void Integrate(float deltaTime)
        {
            Position = new Vector2Float(
                Position.X + Velocity.X * deltaTime,
                Position.Y + Velocity.Y * deltaTime);
            Height += Velocity.Z * deltaTime;
        }

        private bool TryHitTarget()
        {
            double hitRadius = Code == "mg_tracer" ? HitRadiusFastTracer : HitRadiusUnit;

            IAttackTarget? target = ResolveLockTarget();
            if (target != null)
            {
                if (target is Construction construction)
                    return HitsConstruction(construction);

                if (SweptHits(
                        target.AimPosition,
                        target.GetBodyHeight(),
                        hitRadius,
                        HitHeightSlop,
                        out Vector2Float hitMap,
                        out double hitH))
                {
                    Position = hitMap;
                    Height = hitH;
                    return true;
                }
            }

            // Фоллбек: исходная точка прицеливания.
            if (SweptHits(TargetMap, TargetHeight, hitRadius, HitHeightSlop, out Vector2Float aimMap, out double aimH))
            {
                Position = aimMap;
                Height = aimH;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Проверка попадания по отрезку prev→current (иначе быстрые трассеры проскакивают хитбокс за один тик).
        /// </summary>
        private bool SweptHits(
            Vector2Float pointMap,
            double pointHeight,
            double radiusMap,
            double heightSlop,
            out Vector2Float hitMap,
            out double hitHeight)
        {
            hitMap = Position;
            hitHeight = Height;

            Vector2Float a = _prevPosition;
            Vector2Float b = Position;
            Vector2Float ab = b - a;
            double abLenSq = ab.X * ab.X + ab.Y * ab.Y;

            double t;
            if (abLenSq < 1e-10)
            {
                t = 0;
            }
            else
            {
                Vector2Float ap = pointMap - a;
                t = Math.Clamp((ap.X * ab.X + ap.Y * ab.Y) / abLenSq, 0.0, 1.0);
            }

            Vector2Float closest = new(a.X + ab.X * t, a.Y + ab.Y * t);
            double distMap = Vector2Float.Distance(closest, pointMap);
            if (distMap > radiusMap)
                return false;

            double closestH = _prevHeight + (Height - _prevHeight) * t;
            if (Math.Abs(closestH - pointHeight) > heightSlop)
                return false;

            hitMap = closest;
            hitHeight = closestH;
            return true;
        }

        private bool HitsConstruction(Construction construction)
        {
            Vector2Int origin = construction.Position.ToInt();
            int sx = Math.Max(1, construction.Size.X);
            int sy = Math.Max(1, construction.Size.Y);

            const double pad = 0.45;
            double minX = origin.X - pad;
            double maxX = origin.X + sx + pad;
            double minY = origin.Y - pad;
            double maxY = origin.Y + sy + pad;

            if (Position.X < minX || Position.X > maxX || Position.Y < minY || Position.Y > maxY)
                return false;

            double bodyH = construction.GetBodyHeight();
            return Height <= bodyH + 0.85 && Height >= -0.2;
        }

        private bool HasHitGround()
        {
            return Height <= GroundLevel && Velocity.Z <= 0;
        }

        public override void Destroy()
        {
            BaseExplosion explosion = new(
                Range,
                Damage,
                Position,
                _game.Map,
                _game.Constructions,
                _game.Units,
                ownerId: OwnerId,
                ignoreUnitId: _shooterUnitId,
                armorDamage: _armorDamage);
            explosion.Explode();
            base.Destroy();
        }
    }
}
