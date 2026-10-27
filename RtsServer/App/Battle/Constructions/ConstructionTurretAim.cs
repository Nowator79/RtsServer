using RtsServer.App.Battle.Dto;

namespace RtsServer.App.Battle.Constructions
{
    /// <summary>Поворот огневой точки постройки к цели — та же логика, что у юнитных AttackPoint.</summary>
    public static class ConstructionTurretAim
    {
        private const double KRotationSpeed = 2;

        /// <summary>true, когда турель смотрит на цель (угол &lt; alignDeg).</summary>
        public static bool RotateToward(
            ref double rotationDeg,
            Vector2Float from,
            Vector2Float target,
            double speedDegPerSec,
            double deltaTime,
            double alignDeg = 1.0)
        {
            Vector2Float globalFacing = Vector2Float.VectorByVectorAndAngle(from, -rotationDeg);
            Vector2Float localFacing = Vector2Float.VectorByAngle(-rotationDeg);

            double angleToTarget = Vector2Float.AngleByVectorsAndRot(from, localFacing.Normalize(), target);
            double side = Vector2Float.SideByVector(from, globalFacing, target);

            double rotationStep = speedDegPerSec * KRotationSpeed * deltaTime;
            rotationStep = Math.Min(rotationStep, angleToTarget);

            if (side > 0)
                rotationDeg -= rotationStep;
            else if (side < 0)
                rotationDeg += rotationStep;

            return !double.IsNaN(angleToTarget) && Math.Abs(angleToTarget) < alignDeg;
        }
    }
}
