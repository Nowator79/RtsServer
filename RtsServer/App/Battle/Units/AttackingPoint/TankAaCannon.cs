using RtsServer.App.Battle.Dto;
using RtsServer.App.Battle.Interfaces;

namespace RtsServer.App.Battle.Units.AttackingPoint
{
    /// <summary>ПВО-танк: самонаводящиеся AAM только по авиации.</summary>
    public class TankAaCannon(Unit currentUnit, float rotation = 0) : BaseAttackingPoint(currentUnit, rotation)
    {
        public override void Init()
        {
            SpeedRotation = 90;
            Range = 32;
            fireCooldown = 1.2;
            AllowedTargetDomains = AttackTargetDomain.Air;
            ArmorDamage = new ArmorDamageProfile(vsLight: 288f, vsMedium: 180f, vsHeavy: 84f);
            Damage = ArmorDamage.VsLight;
        }

        protected override void Attack()
        {
            if (TypeTargetUnit != TypeTarget.Unit || Target == null)
                return;

            if (!RotationToTarget(Target.AimPosition))
                return;

            TryFireAt(
                Target.AimPosition,
                missileCode: "aam_rocket",
                missileSpeed: 11f,
                explosionRange: 1.6f,
                guided: true);
        }
    }
}
