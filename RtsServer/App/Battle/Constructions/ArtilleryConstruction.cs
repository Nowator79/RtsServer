using RtsServer.App.Battle.Dto;
using RtsServer.App.Battle.Interfaces;
using RtsServer.App.Battle.MapBattle;
using RtsServer.App.Battle.Tools;
using RtsServer.App.Battle.Units;

namespace RtsServer.App.Battle.Constructions
{
    /// <summary>
    /// Артиллерия 1×1: залп 4 ракет роем (низкая дуга в землю), без наводки, с разбросом.
    /// </summary>
    public class ArtilleryConstruction : Construction, IEnergyConsumer
    {
        public const int sizeX = 1;
        public const int sizeY = 1;
        public const int maxHealth = 2200;
        public const string Code = "Artillery";
        public const int BuildCost = 220;
        public const float BuildTime = 9f;

        private const float FireRange = 10f;
        private const float MissileSpeed = 22f;
        private const float ExplosionRange = 2.4f;
        private const int SalvoSize = 4;
        private const double RocketInterval = 0.18;
        private const double SalvoCooldown = 6.0;
        private const double MuzzleHeight = 2.6;
        private const double LoftDuration = 0.16;
        private const double SpreadRadius = 1.8;
        private static readonly ArmorDamageProfile ArmorDamage = new(vsLight: 120f, vsMedium: 340f, vsHeavy: 420f);

        public override ArmorType Armor => ArmorType.Medium;

        private double _lastRocketTime;
        private double _salvoReadyAt;
        private int _rocketsLeftInSalvo;
        private IAttackTarget? _salvoTarget;

        public int EnergyRequired => IsBuilt ? 40 : 0;

        public ArtilleryConstruction(Vector2Int position, int playerOwner)
            : base(maxHealth, maxHealth, position, new(sizeX, sizeY), Code, playerOwner)
        {
        }

        public override double GetBodyHeight() => GetGroundElevation() + 2.4;

        public override void Update()
        {
            base.Update();
            if (!IsBuilt || !IsPowered || Game == null || IsDestroyed)
                return;

            TryAcquireAndFire();
        }

        private void TryAcquireAndFire()
        {
            double now = Game!.TimeSystem.GetTime();

            if (_rocketsLeftInSalvo <= 0)
            {
                if (now < _salvoReadyAt)
                    return;

                IAttackTarget? target = FindNearestEnemyGroundOrBuilding();
                if (target == null)
                    return;

                _salvoTarget = target;
                _rocketsLeftInSalvo = SalvoSize;
                _lastRocketTime = now - RocketInterval; // сразу первая ракета
            }

            if (_salvoTarget == null || _salvoTarget.IsDestroyed || !Game.CanSee(OwnerId, _salvoTarget.AimPosition))
            {
                AbortSalvo(now);
                return;
            }

            double dist = Vector2Float.Distance(AimPosition, _salvoTarget.AimPosition);
            if (dist > FireRange)
            {
                AbortSalvo(now);
                return;
            }

            if (now - _lastRocketTime < RocketInterval)
                return;

            FireRocket(_salvoTarget);
            _lastRocketTime = now;
            _rocketsLeftInSalvo--;

            if (_rocketsLeftInSalvo <= 0)
            {
                _salvoTarget = null;
                _salvoReadyAt = now + SalvoCooldown;
            }
        }

        private void AbortSalvo(double now)
        {
            _rocketsLeftInSalvo = 0;
            _salvoTarget = null;
            _salvoReadyAt = now + 0.5;
        }

        private void FireRocket(IAttackTarget target)
        {
            Vector2Float from = AimPosition;
            Vector2Float aim = ApplySpread(target.AimPosition, SpreadRadius);
            double fromHeight = GetGroundElevation() + MuzzleHeight;
            double toHeight = TerrainHeights.SampleAt(Game, aim) + 0.05;
            float speed = Ballistics.ScaleSpeed(MissileSpeed);
            double loft = Ballistics.ScaleDuration(LoftDuration);
            Vector3Float toAim = new(
                aim.X - from.X,
                aim.Y - from.Y,
                toHeight - fromHeight);
            double mag = toAim.Magnitude();
            Vector3Float velocity = mag > 1e-6
                ? toAim.Normalized() * speed
                : new Vector3Float(0, 0, speed);

            Missile missile = new(
                "artillery_rocket",
                from,
                fromHeight,
                velocity,
                aim,
                toHeight,
                speed,
                ExplosionRange,
                ArmorDamage.VsHeavy,
                Game!.TimeSystem,
                currentAttackPoint: null,
                playerOwner: OwnerId,
                game: Game,
                lockTarget: null,
                guided: false,
                shooterUnitId: null,
                applyGravity: false,
                armorDamage: ArmorDamage,
                loftDurationSeconds: loft,
                sharpDivertAfterLoft: true);

            Game.AddMissile(missile);
        }

        private static Vector2Float ApplySpread(Vector2Float center, double radius)
        {
            double angle = Random.Shared.NextDouble() * Math.PI * 2.0;
            double dist = Math.Sqrt(Random.Shared.NextDouble()) * radius;
            return new Vector2Float(
                center.X + Math.Cos(angle) * dist,
                center.Y + Math.Sin(angle) * dist);
        }

        private IAttackTarget? FindNearestEnemyGroundOrBuilding()
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

            foreach (Construction building in Game.Constructions)
            {
                if (building == null || building.IsDestroyed || !building.IsBuilt) continue;
                if (building.OwnerId == OwnerId || building.OwnerId < 0) continue;
                if (!Game.CanSee(OwnerId, building.AimPosition)) continue;

                double dist = Vector2Float.Distance(AimPosition, building.AimPosition);
                if (dist >= bestDist) continue;
                bestDist = dist;
                best = building;
            }

            return best;
        }
    }
}
