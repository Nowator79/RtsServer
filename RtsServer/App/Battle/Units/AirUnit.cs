using RtsServer.App.Battle.Dto;

namespace RtsServer.App.Battle.Units
{
    /// <summary>
    /// Летающий юнит: не останавливается, летит к цели и при сближении уходит на кружение вокруг неё.
    /// </summary>
    public class AirUnit : Unit
    {
        private enum FlightMode
        {
            FlyToTarget,
            Orbit
        }

        private FlightMode _flightMode = FlightMode.FlyToTarget;
        private Vector2Float _orbitCenter;

        private double _orbitRadius = 5.0;
        private double _orbitEnterDistance = 7.0;
        private double _orbitExitDistance = 10.0;

        public AirUnit(Vector2Float position, int playerOwner)
            : base("AirUnit", 800, 800, position, playerOwner)
        {
            MaxSpeed = 3.0;
            AccelerationForce = 8.0;
            RotationSpeed = 90.0;
        }

        /// <summary>
        /// Для самолёта целевая позиция — центр орбиты.
        /// Навигатор/грид-маршрут не используются, самолёт летит напрямую.
        /// </summary>
        public override Unit SetTargetPosition(Vector2Int targetPosition)
        {
            if (targetPosition.X < 0 || targetPosition.Y < 0) return this;

            TargetPosition = targetPosition;
            _orbitCenter = targetPosition.GetFloat();
            _flightMode = FlightMode.FlyToTarget;
            return this;
        }

        /// <summary>
        /// Пользовательское движение самолёта: без остановок, с режимами полёт к цели / кружение.
        /// </summary>
        protected override void MoveToTarget()
        {
            double deltaTime = Game.TimeSystem.GetDelta();
            if (deltaTime <= 0)
                return;

            // Если цель ещё не задана, просто летим по текущему курсу.
            if (TargetPosition.X < 0 || TargetPosition.Y < 0)
            {
                FlyForward(deltaTime);
                return;
            }

            Vector2Float toCenter = _orbitCenter - Position;
            double dist = Vector2Float.Distance(Position, _orbitCenter);

            // Переключение режимов
            switch (_flightMode)
            {
                case FlightMode.FlyToTarget:
                    if (dist < _orbitEnterDistance)
                        _flightMode = FlightMode.Orbit;
                    break;

                case FlightMode.Orbit:
                    if (dist > _orbitExitDistance)
                        _flightMode = FlightMode.FlyToTarget;
                    break;
            }

            Vector2Float desiredDir;
            if (dist < 0.001)
            {
                desiredDir = GetForward2D();
            }
            else
            {
                switch (_flightMode)
                {
                    case FlightMode.FlyToTarget:
                        desiredDir = toCenter.Normalize();
                        break;

                    case FlightMode.Orbit:
                        Vector2Float radial = toCenter.Normalize();
                        // касательная по часовой стрелке
                        Vector2Float tangential = new Vector2Float(-radial.Y, radial.X);

                        // коррекция радиуса (чтобы держаться около _orbitRadius)
                        double radiusError = dist - _orbitRadius;
                        radiusError = Math.Clamp(radiusError, -1.0, 1.0);
                        Vector2Float radiusCorrection = radial * (float)(-radiusError * 0.4);

                        desiredDir = (tangential + radiusCorrection).Normalize();
                        break;

                    default:
                        desiredDir = GetForward2D();
                        break;
                }
            }

            RotateTowards(desiredDir, deltaTime);
            UpdateSpeed(deltaTime);
            FlyForward(deltaTime);
        }

        private Vector2Float GetForward2D()
        {
            // В базовом Unit поворот считается так: VectorByAngle(-Rotation)
            return Vector2Float.VectorByAngle(-Rotation).Normalize();
        }

        private void RotateTowards(Vector2Float desiredDir, double deltaTime)
        {
            Vector2Float forward = GetForward2D();
            Vector2Float targetDir = desiredDir.Normalize();

            double angleToTarget = Vector2Float.AngleByVecotrs(forward, targetDir);
            double rotationDirection = Math.Sign(Vector2Float.Cross(forward, targetDir));

            double rotationStep = RotationSpeed * deltaTime;
            Rotation += -rotationDirection * Math.Min(rotationStep, angleToTarget);
        }

        private void UpdateSpeed(double deltaTime)
        {
            IsMoving = true;
            CurrentSpeed = Math.Min(CurrentSpeed + AccelerationForce * deltaTime, MaxSpeed);
        }

        private void FlyForward(double deltaTime)
        {
            if (CurrentSpeed <= 0)
                return;

            Vector2Float forward = GetForward2D();
            Position += forward * (float)(CurrentSpeed * deltaTime);
        }
    }
}

