using RtsServer.App.Battle.Dto;

namespace RtsServer.App.Battle.Units.AttackingPoint
{
    /// <summary>
    /// Носовая пушка самолёта: стреляет, когда цель в радиусе и примерно по курсу.
    /// </summary>
    public class AirCannon(Unit currentUnit, float rotation = 0) : BaseAttackingPoint(currentUnit, rotation)
    {
        private const double FireAngleDegrees = 30.0;

        public override void Init()
        {
            Damage = 400;
            SpeedRotation = 0;
            Range = 9;
            fireCooldown = 1.2;
            _lastShotTime = 0;
        }

        protected override void Attack()
        {
            if (TargetUnit == null)
                return;

            // Визуально орудие смотрит по курсу самолёта.
            Rotation = CurrentUnit.Rotation;

            Vector2Float forward = Vector2Float.VectorByAngle(-CurrentUnit.Rotation).Normalize();
            Vector2Float toTarget = (TargetUnit.Position - CurrentUnit.Position).Normalize();
            double angleToTarget = Vector2Float.AngleByVecotrs(forward, toTarget);

            if (double.IsNaN(angleToTarget) || angleToTarget > FireAngleDegrees)
                return;

            // Снаряд чуть быстрее и с меньшим радиусом взрыва, чем у танка.
            TryFireAt(TargetUnit.Position, missileCode: "tank_shell", missileSpeed: 28f, explosionRange: 1.5f);
        }
    }
}
