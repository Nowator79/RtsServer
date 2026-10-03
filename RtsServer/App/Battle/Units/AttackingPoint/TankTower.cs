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
            fireCooldown = 5;
            // По Docs/UnitAttackDomains.md: Ground | Construction.
            AllowedTargetDomains = AttackTargetDomain.Ground | AttackTargetDomain.Construction;
            ArmorDamage = new ArmorDamageProfile(vsLight: 154f, vsMedium: 490f, vsHeavy: 735f);
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
