using RtsServer.App.Battle.Dto;
using RtsServer.App.Battle.Interfaces;

namespace RtsServer.App.Battle.Units.AttackingPoint
{
    /// <summary>Автомат пехотинца (верхняя часть тела) — земля и здания.</summary>
    public class SoldierRifle(Unit currentUnit, float rotation = 0) : BaseAttackingPoint(currentUnit, rotation)
    {
        private const int BurstSize = 3;
        private const double ShotInterval = 0.12;
        private const double BurstPause = 0.55;

        private int _shotsInBurst;
        private double _pauseUntil;

        public override void Init()
        {
            SpeedRotation = 220;
            Range = 7;
            fireCooldown = ShotInterval;
            _lastShotTime = 0;
            _shotsInBurst = 0;
            _pauseUntil = 0;
            AllowedTargetDomains = AttackTargetDomain.Ground | AttackTargetDomain.Construction;
            ArmorDamage = new ArmorDamageProfile(vsLight: 52f, vsMedium: 20f, vsHeavy: 5f);
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

            if (!TryFireAt(Target.AimPosition, missileCode: "mg_tracer", missileSpeed: 22f, explosionRange: 0.75f, flatTrajectory: true))
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
