using RtsServer.App.Battle.Dto;
using RtsServer.App.Battle.Interfaces;

namespace RtsServer.App.Battle.Units.AttackingPoint
{
    /// <summary>AGM — только земля и здания. Не переключается на воздух.</summary>
    public class AgmAirCannon(Unit currentUnit, float rotation = 0) : BaseAttackingPoint(currentUnit, rotation)
    {
        private const double FireAngleDegrees = 34.0;
        private const double FireMaxDistance = 18.0;

        public override void Init()
        {
            SpeedRotation = 0;
            Range = 40;
            fireCooldown = 0.28;
            AllowedTargetDomains = AttackTargetDomain.Ground | AttackTargetDomain.Construction;
            ArmorDamage = new ArmorDamageProfile(vsLight: 98f, vsMedium: 238f, vsHeavy: 308f);
            Damage = ArmorDamage.VsHeavy;
        }

        protected override void Attack()
        {
            if (Target == null)
                return;

            if (CurrentUnit is not AirUnit air || !air.IsStrafing)
                return;

            if (!air.CanFireRocket())
                return;

            Rotation = CurrentUnit.Rotation;

            double dist = Vector2Float.Distance(CurrentUnit.Position, Target.AimPosition);
            if (dist > FireMaxDistance)
                return;

            Vector2Float forward = Vector2Float.VectorByAngle(-CurrentUnit.Rotation).Normalize();
            Vector2Float toTarget = (Target.AimPosition - CurrentUnit.Position).Normalize();
            double angleToTarget = Vector2Float.AngleByVecotrs(forward, toTarget);

            if (double.IsNaN(angleToTarget) || angleToTarget > FireAngleDegrees)
                return;

            if (TryFireAt(
                    Target.AimPosition,
                    missileCode: "agm_rocket",
                    missileSpeed: 12f,
                    explosionRange: 1.8f,
                    guided: true))
            {
                air.TryConsumeRocket();
                air.NotifyShotFired();
            }
        }
    }
}
