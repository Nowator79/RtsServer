using RtsServer.App.Battle.Dto;
using RtsServer.App.Battle.Interfaces;

namespace RtsServer.App.Battle.Units.AttackingPoint
{
    /// <summary>
    /// Ракетный контейнер вертолёта: залп 6 ракет по 2 шт. каждые 0.3 с, затем КД 5 с.
    /// </summary>
    public class HelicopterGun(Unit currentUnit, float rotation = 0) : BaseAttackingPoint(currentUnit, rotation)
    {
        private const int SalvoSize = 6;
        private const int RocketsPerBurst = 2;
        private const double BurstInterval = 0.3;
        private const double SalvoCooldown = 5.0;

        private int _rocketsLeftInSalvo;
        private double _nextBurstAt;
        private double _salvoReadyAt;

        public override void Init()
        {
            SpeedRotation = 160;
            Range = 14;
            fireCooldown = SalvoCooldown;
            _lastShotTime = 0;
            AllowedTargetDomains = AttackTargetDomain.Ground | AttackTargetDomain.Air | AttackTargetDomain.Construction;
            ArmorDamage = new ArmorDamageProfile(vsLight: 160f, vsMedium: 95f, vsHeavy: 55f);
            Damage = ArmorDamage.VsLight;
        }

        protected override void Attack()
        {
            if (TypeTargetUnit != TypeTarget.Unit || Target == null)
                return;

            if (!RotationToTarget(Target.AimPosition))
                return;

            double now = CurrentUnit.Game.TimeSystem.GetTime();

            if (_rocketsLeftInSalvo <= 0)
            {
                if (now < _salvoReadyAt)
                    return;

                _rocketsLeftInSalvo = SalvoSize;
                _nextBurstAt = now;
            }

            if (now < _nextBurstAt)
                return;

            string missileCode = Target.AttackDomain == AttackTargetDomain.Air
                ? "aam_rocket"
                : "agm_rocket";

            int fired = 0;
            for (int i = 0; i < RocketsPerBurst && _rocketsLeftInSalvo > 0; i++)
            {
                Vector2Float aim = ApplyPairSpread(Target.AimPosition, i);

                if (!TryFireAt(
                        aim,
                        missileCode: missileCode,
                        missileSpeed: 16f,
                        explosionRange: 1.5f,
                        guided: true,
                        ignoreCooldown: true))
                    break;

                _rocketsLeftInSalvo--;
                fired++;
            }

            if (fired <= 0)
                return;

            _lastShotTime = now;
            _nextBurstAt = now + BurstInterval;

            if (_rocketsLeftInSalvo <= 0)
                _salvoReadyAt = now + SalvoCooldown;
        }

        /// <summary>Небольшой разнос пары ракет влево/вправо от цели.</summary>
        private static Vector2Float ApplyPairSpread(Vector2Float center, int pairIndex)
        {
            const double offset = 0.22;
            double sign = pairIndex == 0 ? -1.0 : 1.0;
            return new Vector2Float(center.X + sign * offset, center.Y);
        }
    }
}
