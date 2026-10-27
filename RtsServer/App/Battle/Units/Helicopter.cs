using RtsServer.App.Battle.Dto;
using RtsServer.App.Battle.Interfaces;
using RtsServer.App.Battle.Navigator;
using RtsServer.App.Battle.Units.AttackingPoint;

namespace RtsServer.App.Battle.Units
{
    /// <summary>
    /// Вертолёт: летит напрямую к точке приказа (без A* по клеткам), над препятствиями.
    /// Режим низкого полёта: невидим для ПВО, медленнее, уязвим к наземному оружию.
    /// </summary>
    public class Helicopter : Unit
    {
        public const double FlightAltitude = 4.2;
        public const double LowFlightAltitude = 1.35;
        private const double HighMaxSpeed = 2.4;
        private const double LowMaxSpeed = 1.05;
        private const double ArriveDistance = 0.35;
        /// <summary>Скорость смены клиренса над поверхностью (мир. ед./с).</summary>
        private const double ClearanceChangeSpeed = 2.8;

        private Vector2Float _cruisePoint;
        private bool _hasCruisePoint;
        private double _currentClearance = FlightAltitude;

        public bool IsLowFlight { get; private set; }

        public override bool OccupiesGroundCell => false;
        public override bool FliesOverObstacles => true;
        public override double CollisionRadius => 0.65;
        public override AttackTargetDomain AttackDomain =>
            IsLowFlight ? AttackTargetDomain.Ground : AttackTargetDomain.Air;
        public override ArmorType Armor => ArmorType.Light;

        /// <summary>Высота = рельеф под машиной + текущий клиренс (плавно к high/low).</summary>
        public override double GetBodyHeight() => GetGroundElevation() + _currentClearance;
        public override double GetMuzzleHeight() => GetBodyHeight();

        public Helicopter(Vector2Float position, int playerOwner)
            : base("Helicopter", 700, 700, position, playerOwner)
        {
            MaxSpeed = HighMaxSpeed;
            RotationSpeed = 150;
            AccelerationForce = 22;
            AttackingPoints = [new HelicopterMachineGun(this), new HelicopterGun(this)];
            _cruisePoint = position;
            _hasCruisePoint = true;
            _currentClearance = FlightAltitude;
        }

        public void SetLowFlight(bool enabled)
        {
            if (IsLowFlight == enabled)
                return;

            IsLowFlight = enabled;
            MaxSpeed = enabled ? LowMaxSpeed : HighMaxSpeed;
            if (CurrentSpeed > MaxSpeed)
                CurrentSpeed = MaxSpeed;
        }

        public void ToggleLowFlight() => SetLowFlight(!IsLowFlight);

        public override Unit SetTargetPosition(Vector2Int targetPosition, bool attackMove = false)
        {
            return SetTargetPosition(targetPosition, attackMove, reservations: null);
        }

        public override Unit SetTargetPosition(
            Vector2Int targetPosition,
            bool attackMove,
            PathReservationMap? reservations)
        {
            if (targetPosition.X < 0 || targetPosition.Y < 0)
                return this;

            TargetPosition = targetPosition;
            IsAttackMoving = attackMove;
            CanAutoAcquireTargets = true;
            _cruisePoint = targetPosition.GetFloat();
            ClampCruisePointInsideMap();
            _hasCruisePoint = true;
            PathRoute?.Clear();
            CurrentSpeed = Math.Min(CurrentSpeed, MaxSpeed * 0.5);

            if (CurrentSpeed < 0.15)
                FaceTowards(_cruisePoint);

            return this;
        }

        public override Unit ApplyPreparedRoute(
            Vector2Int destination,
            IEnumerable<Vector2Int> routeCells,
            bool attackMove = false)
        {
            return SetTargetPosition(destination, attackMove);
        }

        public override void Update()
        {
            CheckAttackTarget();

            foreach (BaseAttackingPoint attackPoint in AttackingPoints)
                attackPoint.Update();

            UpdateHoverClearance();
            MoveToTarget();
            UpdateChunkPosition();
        }

        private void UpdateHoverClearance()
        {
            if (Game == null)
                return;

            double deltaTime = Game.TimeSystem.GetDelta();
            if (deltaTime <= 0)
                return;

            double target = IsLowFlight ? LowFlightAltitude : FlightAltitude;
            double step = ClearanceChangeSpeed * deltaTime;
            if (_currentClearance < target)
                _currentClearance = Math.Min(target, _currentClearance + step);
            else if (_currentClearance > target)
                _currentClearance = Math.Max(target, _currentClearance - step);
        }

        protected override void MoveToTarget()
        {
            if (Game == null)
                return;

            double deltaTime = Game.TimeSystem.GetDelta();
            if (deltaTime <= 0)
                return;

            if (!_hasCruisePoint)
            {
                _cruisePoint = Position;
                _hasCruisePoint = true;
            }

            Vector2Float toTarget = _cruisePoint - Position;
            double dist = toTarget.Magnitude();

            if (dist <= ArriveDistance)
            {
                Position = _cruisePoint;
                CurrentSpeed = 0;
                IsMoving = false;
                return;
            }

            Vector2Float desiredDir = toTarget.Normalize();
            desiredDir = ApplyMapBoundsSteering(desiredDir);
            RotateTowards(desiredDir, deltaTime);

            double stopDist = Math.Max(ArriveDistance, CurrentSpeed * CurrentSpeed / (2.0 * Math.Max(0.1, AccelerationForce)));
            if (dist < stopDist)
                CurrentSpeed = Math.Max(0, CurrentSpeed - AccelerationForce * deltaTime);
            else
                CurrentSpeed = Math.Min(CurrentSpeed + AccelerationForce * deltaTime, MaxSpeed);

            IsMoving = CurrentSpeed > 0.01;
            if (!IsMoving)
                return;

            Vector2Float forward = GetForward2D();
            Position += forward * (float)(CurrentSpeed * deltaTime);
            ClampPositionToMap();
        }

        protected override bool IsIdleForAggro() =>
            _hasCruisePoint && Vector2Float.Distance(Position, _cruisePoint) <= ArriveDistance * 2.0;

        private void FaceTowards(Vector2Float point)
        {
            Vector2Float dir = point - Position;
            if (dir.Magnitude() < 0.01)
                return;
            double angle = Math.Atan2(dir.Y, dir.X) * (180.0 / Math.PI);
            Rotation = -angle;
        }

        private Vector2Float GetForward2D() =>
            Vector2Float.VectorByAngle(-Rotation).Normalize();

        private void RotateTowards(Vector2Float desiredDir, double deltaTime)
        {
            Vector2Float forward = GetForward2D();
            Vector2Float targetDir = desiredDir.Normalize();
            if (targetDir.Magnitude() < 0.001)
                return;

            double angleToTarget = Vector2Float.AngleByVecotrs(forward, targetDir);
            double rotationDirection = Math.Sign(Vector2Float.Cross(forward, targetDir));
            if (rotationDirection == 0 && angleToTarget > 0.1)
                rotationDirection = 1;

            double rotationStep = RotationSpeed * deltaTime;
            Rotation += -rotationDirection * Math.Min(rotationStep, angleToTarget);
        }

        private Vector2Float ApplyMapBoundsSteering(Vector2Float desiredDir)
        {
            if (Game?.Map == null)
                return desiredDir;

            double min = 0.5;
            double maxX = Game.Map.Width - 0.5;
            double maxY = Game.Map.Length - 0.5;
            const double margin = 3.0;

            bool outside =
                Position.X < min || Position.X > maxX ||
                Position.Y < min || Position.Y > maxY;
            bool nearEdge =
                Position.X < min + margin || Position.X > maxX - margin ||
                Position.Y < min + margin || Position.Y > maxY - margin;

            if (!outside && !nearEdge)
                return desiredDir;

            Vector2Float mapCenter = new(Game.Map.Width * 0.5, Game.Map.Length * 0.5);
            Vector2Float toCenter = mapCenter - Position;
            if (toCenter.Magnitude() < 0.001)
                return desiredDir;

            Vector2Float inward = toCenter.Normalize();
            return outside ? inward : (desiredDir + inward * 2.0).Normalize();
        }

        private void ClampCruisePointInsideMap()
        {
            if (Game?.Map == null)
                return;

            double min = 0.5;
            double maxX = Math.Max(min, Game.Map.Width - 0.5);
            double maxY = Math.Max(min, Game.Map.Length - 0.5);
            _cruisePoint = new Vector2Float(
                Math.Clamp(_cruisePoint.X, min, maxX),
                Math.Clamp(_cruisePoint.Y, min, maxY));
        }

        private void ClampPositionToMap()
        {
            if (Game?.Map == null)
                return;

            double min = 0.5;
            double maxX = Math.Max(min, Game.Map.Width - 0.5);
            double maxY = Math.Max(min, Game.Map.Length - 0.5);
            double x = Math.Clamp(Position.X, min, maxX);
            double y = Math.Clamp(Position.Y, min, maxY);
            if (Math.Abs(x - Position.X) > 0.0001 || Math.Abs(y - Position.Y) > 0.0001)
                Position = new Vector2Float(x, y);
        }
    }
}
