using RtsServer.App.Battle.Dto;
using RtsServer.App.Battle.Interfaces;

namespace RtsServer.App.Battle.Units.AttackingPoint
{
    /// <summary>
    /// Пулемёт: очередь из 5 выстрелов, затем пауза 1 секунда.
    /// </summary>
    public class BaggyMachineGun(Unit currentUnit, float rotation = 0) : BaseAttackingPoint(currentUnit, rotation)
    {
        private const int BurstSize = 5;
        private const double ShotInterval = 0.16;
        private const double BurstPause = 1.0;

        private int _shotsInBurst;
        private double _pauseUntil;

        public override void Init()
        {
            SpeedRotation = 140;
            Range = 9;
            fireCooldown = ShotInterval;
            _lastShotTime = 0;
            _shotsInBurst = 0;
            _pauseUntil = 0;
            // Багги бьют землю, воздух и здания.
            AllowedTargetDomains = AttackTargetDomain.Ground | AttackTargetDomain.Air | AttackTargetDomain.Construction;
            ArmorDamage = new ArmorDamageProfile(vsLight: 138f, vsMedium: 66f, vsHeavy: 17f);
            Damage = ArmorDamage.VsLight;
        }

        protected override void Attack()
        {
            if (TypeTargetUnit != TypeTarget.Unit || Target == null)
                return;

            double now = CurrentUnit.Game.TimeSystem.GetTime();
            if (now < _pauseUntil)
                return;

            if (!RotationToTarget(Target.AimPosition))
                return;

            if (!TryFireAt(
                    Target.AimPosition,
                    missileCode: "mg_tracer",
                    missileSpeed: 22f,
                    explosionRange: 0.9f,
                    flatTrajectory: true))
                return;

            _shotsInBurst++;
            if (_shotsInBurst < BurstSize)
                return;

            _shotsInBurst = 0;
            _pauseUntil = now + BurstPause;
        }

        protected override void LoseTarget()
        {
            base.LoseTarget();
            _shotsInBurst = 0;
            _pauseUntil = 0;
        }
    }
}
