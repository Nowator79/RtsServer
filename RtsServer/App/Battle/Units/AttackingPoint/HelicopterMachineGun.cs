using RtsServer.App.Battle.Dto;
using RtsServer.App.Battle.Interfaces;

namespace RtsServer.App.Battle.Units.AttackingPoint
{
    /// <summary>
    /// Пулемёт Гидры: непрерывный hitscan без паузы между очередями.
    /// </summary>
    public class HelicopterMachineGun(Unit currentUnit, float rotation = 0) : BaseAttackingPoint(currentUnit, rotation)
    {
        private const double ShotInterval = 0.12;

        public override void Init()
        {
            SpeedRotation = 200;
            Range = 11;
            fireCooldown = ShotInterval;
            _lastShotTime = 0;
            AllowedTargetDomains = AttackTargetDomain.Ground | AttackTargetDomain.Air | AttackTargetDomain.Construction;
            ArmorDamage = new ArmorDamageProfile(vsLight: 72f, vsMedium: 28f, vsHeavy: 9f);
            Damage = ArmorDamage.VsLight;
        }

        protected override void Attack()
        {
            if (TypeTargetUnit != TypeTarget.Unit || Target == null)
                return;

            if (!RotationToTarget(Target.AimPosition))
                return;

            TryHitscanAt(Target);
        }
    }
}
