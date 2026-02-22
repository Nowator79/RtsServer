using RtsServer.App.Battle.Dto;
using RtsServer.App.Battle.Units;

namespace RtsServer.App.Battle.Units.AttackingPoint
{
    public class BaseAttackingPoint(Unit currentUnit, float rotation = 0)
    {
        public Unit CurrentUnit { get; protected set; } = currentUnit;
        public double Rotation { get; protected set; } = rotation;
        public Vector2Float? TargetPoint { get; protected set; }
        public Unit? TargetUnit { get; protected set; }
        public float Damage { get; protected set; }
        public double SpeedRotation { get; protected set; }
        public float Range { get; protected set; }
        protected TypeTarget TypeTargetUnit { get; set; } = TypeTarget.Empty;

        protected const int KRotationSpeed = 1;

        protected double fireCooldown = 1.0;
        private double _lastShotTime = 0;

        public virtual void Init()
        {
            Damage = 0;
            SpeedRotation = 0;
            Range = 0;
            fireCooldown = 1.0;
            _lastShotTime = 0;
        }

        public void Update()
        {
            // Потеря цели, если она вышла за пределы радиуса
            if (
                TargetUnit != null &&
                !Vector2Float.ReachDistance(CurrentUnit.Position, TargetUnit.Position, Range)
            )
            {
                TargetUnit.OnDestroyAction -= LoseTarget;
                TargetUnit = null;
                TypeTargetUnit = TypeTarget.Empty;
            }

            // Потеря уничтоженное цели
            if (TargetUnit != null && TargetUnit.IsDestroyed)
            {
                LoseTarget();
            }

            // Потеря цели, если точка обнулена
            if (
                TargetUnit == null &&
                TargetPoint.HasValue &&
                TargetPoint.Value != Vector2Float.Zero &&
                TypeTargetUnit != TypeTarget.Empty
            )
            {
                TypeTargetUnit = TypeTarget.Empty;
            }

            // Если нет цели, вращаем башню к направлению корпуса
            if (TargetUnit == null && TypeTargetUnit == TypeTarget.Empty)
            {
                RotateToUnitDirection();
            }

            Attack();
        }

        protected virtual void Attack()
        {
            if (TypeTargetUnit == TypeTarget.Unit && TargetUnit != null)
            {
                if (RotationToTarget(TargetUnit.Position))
                {
                    double now = CurrentUnit.Game.TimeSystem.GetTime();
                    if (now - _lastShotTime >= fireCooldown)
                    {
                        Missile missile = new("tank_shell", CurrentUnit.Position, TargetUnit.Position, 20, 2, Damage, CurrentUnit.Game.TimeSystem, this, CurrentUnit.OwnerId);
                        CurrentUnit.Game.AddMissile(missile);
                        _lastShotTime = now;
                    }
                }
            }
        }

        public virtual void CheckTarget()
        {
            if (TargetUnit != null) return;
            foreach (Unit unit in CurrentUnit.Game.Units)
            {
                if (unit == CurrentUnit) continue;
                if (unit.OwnerId == CurrentUnit.OwnerId) continue;
                if (Vector2Float.ReachDistance(CurrentUnit.Position, unit.Position, Range))
                {
                    SetTarget(unit);
                    break;
                }
            }
        }

        public virtual void SetTarget(Vector2Float targetPoint)
        {
            TargetPoint = targetPoint;
            TargetUnit = null;
            TypeTargetUnit = TypeTarget.Position;
        }

        public virtual void SetTarget(Unit targetUnit)
        {
            TargetPoint = null;
            TargetUnit = targetUnit;
            targetUnit.OnDestroyAction += LoseTarget;
            TypeTargetUnit = TypeTarget.Unit;
        }

        protected virtual void LoseTarget()
        {
            TargetUnit = null;
            TargetPoint = null;
            TypeTargetUnit = TypeTarget.Empty;
        }

        protected enum TypeTarget
        {
            Position,
            Unit,
            Empty
        }

        public bool RotationToTarget(Vector2Float target)
        {
            Vector2Float globalFacing = Vector2Float.VectorByVectorAndAngle(CurrentUnit.Position, -Rotation);
            Vector2Float localFacing = Vector2Float.VectorByAngle(-Rotation);

            double angleToTarget = Vector2Float.AngleByVectorsAndRot(CurrentUnit.Position, localFacing.Normalize(), target);
            double side = Vector2Float.SideByVector(CurrentUnit.Position, globalFacing, target);

            double deltaTime = CurrentUnit.Game.TimeSystem.GetDelta();
            double rotationStep = SpeedRotation * KRotationSpeed * deltaTime;

            rotationStep = Math.Min(rotationStep, angleToTarget);

            double newAngle = Rotation;

            if (side > 0)
                newAngle -= rotationStep;
            else if (side < 0)
                newAngle += rotationStep;

            Rotation = newAngle;

            return !double.IsNaN(angleToTarget) && Math.Abs(angleToTarget) < 1.0;
        }

        private void RotateToUnitDirection()
        {
            double deltaTime = CurrentUnit.Game.TimeSystem.GetDelta();
            double rotationStep = SpeedRotation * KRotationSpeed * deltaTime;

            double angleDifference = NormalizeAngle(CurrentUnit.Rotation - Rotation);

            if (Math.Abs(angleDifference) < rotationStep)
            {
                Rotation = CurrentUnit.Rotation;
                return;
            }

            Rotation += Math.Sign(angleDifference) * rotationStep;
        }

        private double NormalizeAngle(double angle)
        {
            angle %= 360;
            if (angle < -180) angle += 360;
            if (angle > 180) angle -= 360;
            return angle;
        }
    }
}
