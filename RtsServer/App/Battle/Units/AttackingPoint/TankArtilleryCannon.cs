using RtsServer.App.Battle.Dto;
using RtsServer.App.Battle.Interfaces;
using RtsServer.App.Battle.MapBattle;

namespace RtsServer.App.Battle.Units.AttackingPoint
{
    /// <summary>
    /// Артиллерийский танк: залп 4 ракеты роем (чуть вверх → низкая дуга в землю), без наводки, с разбросом. КД 6 с.
    /// </summary>
    public class TankArtilleryCannon(Unit currentUnit, float rotation = 0) : BaseAttackingPoint(currentUnit, rotation)
    {
        private const double LoftDuration = 0.16;
        private const int SalvoSize = 4;
        private const double RocketInterval = 0.18;
        private const double SalvoCooldown = 6.0;
        private const double SpreadRadius = 2.2;

        private int _rocketsLeftInSalvo;
        private double _nextRocketAt;
        private double _salvoReadyAt;

        public override void Init()
        {
            SpeedRotation = 70;
            Range = 48;
            fireCooldown = SalvoCooldown;
            AllowedTargetDomains = AttackTargetDomain.Ground | AttackTargetDomain.Construction;
            ArmorDamage = new ArmorDamageProfile(vsLight: 120f, vsMedium: 340f, vsHeavy: 420f);
            Damage = ArmorDamage.VsHeavy;
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
                _nextRocketAt = now;
            }

            if (now < _nextRocketAt)
                return;

            Vector2Float aim = ApplySpread(Target.AimPosition, SpreadRadius);

            // Низкая дуга: скорость повыше, чтобы на Range=48 долетало без «высокой» ветки.
            if (!TryFireAt(
                    aim,
                    missileCode: "artillery_rocket",
                    missileSpeed: 26f,
                    explosionRange: 2.4f,
                    targetHeightOverride: TerrainHeights.SampleAt(CurrentUnit.Game, aim) + 0.05,
                    guided: false,
                    loftDurationSeconds: LoftDuration,
                    sharpDivertAfterLoft: true,
                    ignoreCooldown: true))
                return;

            _rocketsLeftInSalvo--;
            _nextRocketAt = now + RocketInterval;

            if (_rocketsLeftInSalvo <= 0)
                _salvoReadyAt = now + SalvoCooldown;
        }

        private static Vector2Float ApplySpread(Vector2Float center, double radius)
        {
            double angle = Random.Shared.NextDouble() * Math.PI * 2.0;
            double dist = Math.Sqrt(Random.Shared.NextDouble()) * radius;
            return new Vector2Float(
                center.X + Math.Cos(angle) * dist,
                center.Y + Math.Sin(angle) * dist);
        }
    }
}
