using RtsServer.App.Battle.Dto;
using RtsServer.App.Battle.Interfaces;
using RtsServer.App.Battle.Tools;
using RtsServer.App.Battle.Units;

namespace RtsServer.App.Battle.Constructions
{
    /// <summary>
    /// ПВО 1×1: турель запускает самонаводящиеся ракеты только по авиации.
    /// Сначала поворачивает огневую точку, потом пуск.
    /// </summary>
    public class AntiAirTurretConstruction : Construction, IEnergyConsumer, IWeaponFxSource
    {
        public const int sizeX = 1;
        public const int sizeY = 1;
        public const int maxHealth = 2500;
        public const string Code = "AntiAirTurret";
        public const int BuildCost = 160;
        public const float BuildTime = 7f;

        private const float FireRange = 36f;
        private const float MissileSpeed = 11f;
        private const float ExplosionRange = 1.6f;
        private const double FireCooldown = 1.15;
        private const double TurretTurnSpeed = 100;
        private const double MuzzleHeight = 2.4;
        private static readonly ArmorDamageProfile ArmorDamage = new(vsLight: 300f, vsMedium: 180f, vsHeavy: 84f);

        public override ArmorType Armor => ArmorType.Medium;

        private double _lastShotTime;
        private double _turretRotation;
        private Unit? _target;

        public int EnergyRequired => IsBuilt ? 35 : 0;

        public int ShotSerial { get; private set; }
        public float ShotAimX { get; private set; }
        public float ShotAimY { get; private set; }
        public float TurretRotation => (float)_turretRotation;

        public AntiAirTurretConstruction(Vector2Int position, int playerOwner)
            : base(maxHealth, maxHealth, position, new(sizeX, sizeY), Code, playerOwner)
        {
        }

        public override double GetBodyHeight() => GetGroundElevation() + 2.2;

        public override void Update()
        {
            base.Update();
            if (!IsBuilt || !IsPowered || Game == null || IsDestroyed)
                return;

            TryAcquireAndFire();
        }

        private void TryAcquireAndFire()
        {
            if (_target == null || _target.IsDestroyed || !CanStillEngage(_target))
                _target = FindNearestEnemyAir();

            if (_target == null)
                return;

            ShotAimX = (float)_target.AimPosition.X;
            ShotAimY = (float)_target.AimPosition.Y;

            double dt = Game!.TimeSystem.GetDelta();
            if (!ConstructionTurretAim.RotateToward(
                    ref _turretRotation, AimPosition, _target.AimPosition, TurretTurnSpeed, dt))
                return;

            double now = Game.TimeSystem.GetTime();
            if (now - _lastShotTime < FireCooldown)
                return;

            float speed = Ballistics.ScaleSpeed(MissileSpeed);
            Vector2Float from = AimPosition;
            double fromHeight = GetGroundElevation() + MuzzleHeight;
            double toHeight = _target.GetBodyHeight();
            Vector3Float toTarget = new(
                _target.AimPosition.X - from.X,
                _target.AimPosition.Y - from.Y,
                toHeight - fromHeight);

            double mag = toTarget.Magnitude();
            Vector3Float velocity = mag > 1e-6
                ? toTarget.Normalized() * speed
                : new Vector3Float(0, 0, speed);

            Missile missile = new(
                "aam_rocket",
                from,
                fromHeight,
                velocity,
                _target.AimPosition,
                toHeight,
                speed,
                ExplosionRange,
                ArmorDamage.VsLight,
                Game.TimeSystem,
                currentAttackPoint: null,
                playerOwner: OwnerId,
                game: Game,
                lockTarget: _target,
                guided: true,
                shooterUnitId: null,
                armorDamage: ArmorDamage);

            Game.AddMissile(missile);
            // ShotSerial не трогаем: у ПВО FX — сама ракета, не hitscan-трассер.
            _lastShotTime = now;
        }

        private bool CanStillEngage(Unit unit)
        {
            if (unit == null || unit.IsDestroyed) return false;
            if (unit.OwnerId == OwnerId) return false;
            if (unit.AttackDomain != AttackTargetDomain.Air) return false;
            if (!Game!.CanSee(OwnerId, unit.AimPosition)) return false;
            return Vector2Float.Distance(AimPosition, unit.AimPosition) <= FireRange;
        }

        private Unit? FindNearestEnemyAir()
        {
            Unit? best = null;
            double bestDist = FireRange;

            foreach (Unit unit in Game!.Units)
            {
                if (!CanStillEngage(unit)) continue;

                double dist = Vector2Float.Distance(AimPosition, unit.AimPosition);
                if (dist >= bestDist) continue;
                bestDist = dist;
                best = unit;
            }

            return best;
        }
    }
}
