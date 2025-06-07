
using RtsServer.App.Battle.Dto;

namespace RtsServer.App.Battle.Units.AttackingPoint
{
    public class BaseAttackingPoint(Unit CurrentUnit, float Rotation = 0)
    {

        public Unit CurrentUnit { get; protected set; } = CurrentUnit;
        public double Rotation { get; protected set; } = Rotation;
        public Vector2Float? TargetPoint { get; protected set; }
        public Unit? TargetUnit { get; protected set; }
        public float Damage { get; protected set; }
        public double SpeedRotation { get; protected set; }
        protected TypeTarget TypeTargetUnit { get; set; } = TypeTarget.Empty;

        protected const int KRotationSpeed = 1;

        public virtual void Init()
        {
            Damage = 0; SpeedRotation = 0;
        }

        public void Update()
        {
            if(TargetUnit == null && TargetPoint != Vector2Float.Zero && TypeTargetUnit != TypeTarget.Empty)
            {
                TypeTargetUnit = TypeTarget.Empty;
            }

            Attack();
        }

        protected virtual void Attack()
        {
            if (TypeTargetUnit == TypeTarget.Unit && TargetUnit != null)
            {
                RotationToTarget(TargetUnit.Position);
            }
        }

        public virtual void SetTarget(Vector2Float TargetPoint)
        {
            this.TargetPoint = TargetPoint;
            this.TargetUnit = null;
            TypeTargetUnit = TypeTarget.Position;
        }

        public virtual void SetTarget(Unit TargetUnit)
        {
            this.TargetPoint = null;
            this.TargetUnit = TargetUnit;
            TypeTargetUnit = TypeTarget.Unit;
        }


        protected enum TypeTarget
        {
            Position,
            Unit,
            Empty
        }
        public bool RotationToTarget(Vector2Float Target)
        {
            Vector2Float GFacting = Vector2Float.VectorByVectorAndAngle(CurrentUnit.Position, -Rotation);// глобальная точка
            Vector2Float GFactingC = Vector2Float.VectorByAngle(-Rotation); // локальная точка
            double AngleToTarget = Vector2Float.AngleByVectorsAndRot(CurrentUnit.Position, GFactingC.Normalize(), Target);
            double typeAngle = Vector2Float.SideByVector(CurrentUnit.Position, GFacting, Target);

            double dTime = CurrentUnit.Game.TimeSystem.GetDelta();
            double upAngle = SpeedRotation * KRotationSpeed * dTime;
            double newAngle = Rotation;
            if (upAngle > AngleToTarget) upAngle = AngleToTarget;
            if (typeAngle > 0)
            {
                newAngle = Rotation - upAngle;
            }
            else if (typeAngle < 0)
            {
                newAngle = Rotation + upAngle;
            }

            if (Math.Abs(newAngle - Rotation) > 30)
            {
                Console.WriteLine("ERROR");
            }
            Rotation = newAngle;

            if (AngleToTarget % 180 < 5 || double.IsNaN(0 / AngleToTarget))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

    }
}
