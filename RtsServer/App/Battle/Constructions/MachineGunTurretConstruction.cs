using RtsServer.App.Battle.Dto;
using RtsServer.App.Battle.Interfaces;
using RtsServer.App.Battle.Units;

namespace RtsServer.App.Battle.Constructions
{
    /// <summary>
    /// Пулемётная турель 1×1: hitscan по пехоте/лёгкой технике, без серверных снарядов.
    /// Сначала поворачивает огневую точку, потом стреляет.
    /// </summary>
    public class MachineGunTurretConstruction : Construction, IEnergyConsumer, IWeaponFxSource
    {
        public const int sizeX = 1;
        public const int sizeY = 1;
        public const int maxHealth = 2000;
        public const string Code = "MachineGunTurret";
        public const int BuildCost = 140;
        public const float BuildTime = 6f;

        private const float FireRange = 14f;
        private const double FireCooldown = 0.11;
        private const double TurretTurnSpeed = 140;
        private static readonly ArmorDamageProfile ArmorDamage =
            new(vsLight: 95f, vsMedium: 28f, vsHeavy: 8f);

        public override ArmorType Armor => ArmorType.Medium;

        public int EnergyRequired => IsBuilt ? 20 : 0;

        public int ShotSerial { get; private set; }
        public float ShotAimX { get; private set; }
        public float ShotAimY { get; private set; }
        public float TurretRotation => (float)_turretRotation;

        private double _turretRotation;
        private double _lastShotTime;
        private IAttackTarget? _target;

        public MachineGunTurretConstruction(Vector2Int position, int playerOwner)
            : base(maxHealth, maxHealth, position, new(sizeX, sizeY), Code, playerOwner)
        {
        }

        public override double GetBodyHeight() => GetGroundElevation() + 1.8;

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
                _target = FindNearestEnemyGround();

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

            float dmg = ArmorDamage.For(_target.Armor);
            if (dmg > 0.01f)
                _target.Health.TakeDamage(dmg);

            ShotSerial++;
            _lastShotTime = now;
        }

        private bool CanStillEngage(IAttackTarget t)
        {
            if (t.IsDestroyed) return false;
            if ((t.AttackDomain & AttackTargetDomain.Ground) == 0
                && (t.AttackDomain & AttackTargetDomain.Construction) == 0)
                return false;
            if (!Game!.CanSee(OwnerId, t.AimPosition)) return false;
            return Vector2Float.Distance(AimPosition, t.AimPosition) <= FireRange;
        }

        private IAttackTarget? FindNearestEnemyGround()
        {
            IAttackTarget? best = null;
            double bestDist = FireRange;

            foreach (Unit unit in Game!.Units)
            {
                if (unit == null || unit.IsDestroyed) continue;
                if (unit.OwnerId == OwnerId) continue;
                if (unit.AttackDomain != AttackTargetDomain.Ground) continue;
                if (!Game.CanSee(OwnerId, unit.AimPosition)) continue;

                double dist = Vector2Float.Distance(AimPosition, unit.AimPosition);
                if (dist >= bestDist) continue;
                bestDist = dist;
                best = unit;
            }

            return best;
        }
    }
}
