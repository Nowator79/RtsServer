using RtsServer.App.Battle.Dto;
using RtsServer.App.Battle.Interfaces;
using RtsServer.App.Battle.Units;

namespace RtsServer.App.Battle.Constructions
{
    /// <summary>
    /// ПВО 1×1: турель запускает самонаводящиеся ракеты только по авиации.
    /// </summary>
    public class AntiAirTurretConstruction : Construction, IEnergyConsumer
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
        private const double MuzzleHeight = 2.4;
        private static readonly ArmorDamageProfile ArmorDamage = new(vsLight: 300f, vsMedium: 180f, vsHeavy: 84f);

        public override ArmorType Armor => ArmorType.Medium;

        private double _lastShotTime;

        public int EnergyRequired => IsBuilt ? 35 : 0;

        public AntiAirTurretConstruction(Vector2Int position, int playerOwner)
            : base(maxHealth, maxHealth, position, new(sizeX, sizeY), Code, playerOwner)
        {
        }

        public override double GetBodyHeight() => 2.2;

        public override void Update()
        {
            base.Update();
            if (!IsBuilt || !IsPowered || Game == null || IsDestroyed)
                return;

            TryAcquireAndFire();
        }

        private void TryAcquireAndFire()
        {
            Unit? target = FindNearestEnemyAir();
            if (target == null)
                return;

            double now = Game!.TimeSystem.GetTime();
            if (now - _lastShotTime < FireCooldown)
                return;

            Vector2Float from = AimPosition;
            double toHeight = target.GetBodyHeight();
            Vector3Float toTarget = new(
                target.AimPosition.X - from.X,
                target.AimPosition.Y - from.Y,
                toHeight - MuzzleHeight);

            double mag = toTarget.Magnitude();
            Vector3Float velocity = mag > 1e-6
                ? toTarget.Normalized() * MissileSpeed
                : new Vector3Float(0, 0, MissileSpeed);

            Missile missile = new(
                "aam_rocket",
                from,
                MuzzleHeight,
                velocity,
                target.AimPosition,
                toHeight,
                MissileSpeed,
                ExplosionRange,
                ArmorDamage.VsLight,
                Game.TimeSystem,
                currentAttackPoint: null,
                playerOwner: OwnerId,
                game: Game,
                lockTarget: target,
                guided: true,
                shooterUnitId: null,
                armorDamage: ArmorDamage);

            Game.AddMissile(missile);
            _lastShotTime = now;
        }

        private Unit? FindNearestEnemyAir()
        {
            Unit? best = null;
            double bestDist = FireRange;

            foreach (Unit unit in Game!.Units)
            {
                if (unit == null || unit.IsDestroyed) continue;
                if (unit.OwnerId == OwnerId) continue;
                if (unit.AttackDomain != AttackTargetDomain.Air) continue;

                double dist = Vector2Float.Distance(AimPosition, unit.AimPosition);
                if (dist >= bestDist) continue;
                bestDist = dist;
                best = unit;
            }

            return best;
        }
    }
}
