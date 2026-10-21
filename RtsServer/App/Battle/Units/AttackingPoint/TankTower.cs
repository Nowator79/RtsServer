using RtsServer.App.Battle.Dto;
using RtsServer.App.Battle.Interfaces;

namespace RtsServer.App.Battle.Units.AttackingPoint
{
    public class TankTower(Unit CurrentUnit, float Rotation = 0) : BaseAttackingPoint(CurrentUnit, Rotation)
    {
        public override void Init()
        {
            SpeedRotation = 60;
            Range = 10;
            fireCooldown = 1.25;
            // По Docs/UnitAttackDomains.md: Ground | Construction.
            AllowedTargetDomains = AttackTargetDomain.Ground | AttackTargetDomain.Construction;
            ArmorDamage = new ArmorDamageProfile(vsLight: 77f, vsMedium: 245f, vsHeavy: 367.5f);
            Damage = ArmorDamage.VsHeavy;
        }

        protected override void Attack()
        {
            if (TypeTargetUnit != TypeTarget.Unit || Target == null)
                return;

            if (!RotationToTarget(Target.AimPosition))
                return;

            // Низкая дуга + выше скорость — долетает на Range без «миномётной» параболы.
            TryFireAt(
                Target.AimPosition,
                missileCode: "tank_shell",
                missileSpeed: 18f,
                explosionRange: 2f,
                preferLowArc: true);
        }
    }
}
