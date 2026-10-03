using RtsServer.App.Battle.Dto;
using RtsServer.App.Battle.Interfaces;

namespace RtsServer.App.Battle.Units.AttackingPoint
{
    /// <summary>Гранатомёт: низкая дуга, дальше и больнее по средней/тяжёлой броне.</summary>
    public class GrenadierLauncher(Unit currentUnit, float rotation = 0) : BaseAttackingPoint(currentUnit, rotation)
    {
        public override void Init()
        {
            SpeedRotation = 140;
            Range = 14;
            fireCooldown = 3.2;
            AllowedTargetDomains = AttackTargetDomain.Ground | AttackTargetDomain.Construction;
            ArmorDamage = new ArmorDamageProfile(vsLight: 180f, vsMedium: 220f, vsHeavy: 170f);
            Damage = ArmorDamage.VsMedium;
        }

        protected override void Attack()
        {
            if (TypeTargetUnit != TypeTarget.Unit || Target == null)
                return;

            if (!RotationToTarget(Target.AimPosition))
                return;

            TryFireAt(
                Target.AimPosition,
                missileCode: "grenade",
                missileSpeed: 17f,
                explosionRange: 1.25f,
                preferLowArc: true);
        }
    }
}
