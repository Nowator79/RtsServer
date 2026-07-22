using Microsoft.Extensions.Logging;
using RtsServer.App.Battle.Dto;
using RtsServer.App.Battle.MapBattle;
using RtsServer.App.Battle.Navigator;
using RtsServer.App.Battle.Units.AttackingPoint;
using RtsServer.App.NetWork.Tcp;
using RtsServer.App.NetWorkDto;
using RtsServer.App.NetWorkResponseSender;

namespace RtsServer.App.Battle.Units
{
    public abstract class Unit : BattleEntity
    {
        private const double RotationThreshold = 5.0;
        private const double PositionThreshold = 0.05;
        private const double SpeedAdjustmentFactor = 1.0;

        public Health Health { get; }
        public Vector2Int CurChunkPosition { get; protected set; }
        public Vector2Int TargetPosition { get; protected set; }
        public double Rotation { get; protected set; }

        public double CurrentSpeed { get; protected set; }
        public double MaxSpeed { get; protected set; } = 1.0;
        public double AccelerationForce { get; protected set; }
        public bool IsMoving { get; protected set; }
        public double RotationSpeed { get; protected set; }
        public void SetId(int id) => Id = id;
        public Queue<Vector2Int> PathRoute { get; private set; }
        protected INavigator Navigator { get; }

        public BaseAttackingPoint[] AttackingPoints { get; protected set; } = Array.Empty<BaseAttackingPoint>();
        public event Action BeforeChunkUpdate;
        public event Action AfterChunkUpdate;

        private Vector2Int? targetPoint;
        private ILogger<Unit> _logger;
        private bool _disposed;

        public Unit(string code, int health, int maxHealth, Vector2Float position, int playerOwner)
            : base(code, playerOwner, position)
        {
            Health = new Health(health, maxHealth, this);
            Navigator = new GroundUnitNavigator();
            Navigator.SetUnit(this);

            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });
            _logger = loggerFactory.CreateLogger<Unit>();
            _logger.LogDebug(position.ToString());
        }

        public override void Init(Game context)
        {
            base.Init(context);
            Navigator.SetMap(Game.Map);

            foreach (var attackPoint in AttackingPoints)
            {
                attackPoint.Init();
            }
        }

        public virtual Unit SetTargetPosition(Vector2Int targetPosition)
        {
            if (targetPosition.X < 0 || targetPosition.Y < 0) return this;

            TargetPosition = targetPosition;
            try
            {
                Navigator.Start();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Navigation error for unit {UnitId}", Id);
            }
            return this;
        }

        public Unit SetAttackTarget(Unit targetUnit)
        {
            if (targetUnit == null) return this;

            foreach (var attackingPoint in AttackingPoints)
            {
                attackingPoint.SetTarget(targetUnit);
            }
            return this;
        }

        public void CheckAttackTarget()
        {
            foreach (BaseAttackingPoint attackingPoint in AttackingPoints)
            {
                attackingPoint.CheckTarget();
            }
        }

        /// <summary>
        /// Старт для построения маршрута: текущая точка движения (waypoint), если есть, иначе позиция юнита.
        /// </summary>
        internal Vector2Int GetPathStartCell() => targetPoint ?? Position.ToInt();

        /// <summary>
        /// Текущая промежуточная точка маршрута (куда юнит едет сейчас), если есть.
        /// </summary>
        internal Vector2Int? GetCurrentWaypoint() => targetPoint;

        /// <summary>
        /// Устанавливает новый маршрут. Сбрасывает текущую точку — юнит возьмёт первую точку из очереди.
        /// </summary>
        public void SetPathRoute(Queue<Vector2Int> route)
        {
            PathRoute = route ?? throw new ArgumentNullException(nameof(route));
            targetPoint = null;
        }

        /// <summary>
        /// Устанавливает маршрут, построенный от текущей waypoint: первая точка маршрута — та, к которой уже едем.
        /// Юнит продолжит до неё, затем пойдёт по остальным точкам. targetPoint не сбрасывается.
        /// </summary>
        internal void SetPathRouteFromWaypoint(Queue<Vector2Int> route)
        {
            if (route == null || route.Count == 0) return;
            route.Dequeue(); // первая точка = текущая waypoint, её не дублируем
            PathRoute = route;
            // targetPoint не трогаем — юнит продолжает ехать к текущей точке, потом по PathRoute
        }

        public async Task UpdatePathRouteAsync(Queue<Vector2Int> route)
        {
            PathRoute = route ?? throw new ArgumentNullException(nameof(route));
            targetPoint = null;
            await Task.CompletedTask;
        }

        protected virtual void MoveToTarget()
        {
            if (!HasNextTargetPoint() || TileIsBlocked())
            {
                StopMoving();
                return;
            }

            double deltaTime = Game.TimeSystem.GetDelta();
            Vector2Float targetPosition = GetCurrentTargetPosition();
            double distanceToTarget = Vector2Float.Distance(Position, targetPosition);

            if (IsReachedTarget(distanceToTarget))
            {
                SnapToTarget(targetPosition);
                AdvanceToNextPoint();
                return;
            }

            IsMoving = true;
            UpdateSpeed(deltaTime);

            if (!RotateTowardsTarget(targetPosition, deltaTime))
                return;

            MoveForwardTowards(targetPosition, deltaTime);
        }

        private bool TileIsBlocked()
        {
            if (targetPoint.HasValue)
                if (Game.Map != null)
                {
                    var tile = Game.Map.GetArrayMap()[targetPoint.Value.X, targetPoint.Value.Y];
                    return tile.UnitsInPoint.Count > 0 && !tile.UnitsInPoint.Contains(this);
                }
            return false;
        }

        private bool HasNextTargetPoint()
        {
            if (targetPoint.HasValue) return true;

            if (PathRoute != null && PathRoute.Count > 0)
            {
                targetPoint = PathRoute.Dequeue();
                return true;
            }

            return false;
        }

        private void StopMoving()
        {
            IsMoving = false;
            CurrentSpeed = 0;
        }

        private Vector2Float GetCurrentTargetPosition() => targetPoint!.Value.GetFloat();

        private bool IsReachedTarget(double distanceToTarget) => distanceToTarget < PositionThreshold;

        private void SnapToTarget(Vector2Float targetPosition)
        {
            Position = targetPosition;
        }

        private void AdvanceToNextPoint()
        {
            targetPoint = null;
        }

        private bool RotateTowardsTarget(Vector2Float target, double deltaTime)
        {
            var globalFacing = Vector2Float.VectorByAngle(-Rotation).Normalize();
            var toTarget = (target - Position).Normalize();

            var angleToTarget = Vector2Float.AngleByVecotrs(globalFacing, toTarget);
            var rotationDirection = Math.Sign(Vector2Float.Cross(globalFacing, toTarget));

            var rotationStep = RotationSpeed * SpeedAdjustmentFactor * deltaTime;
            Rotation += -rotationDirection * Math.Min(rotationStep, angleToTarget);

            return angleToTarget % 180 < RotationThreshold || double.IsNaN(angleToTarget);
        }

        private void MoveForwardTowards(Vector2Float target, double deltaTime)
        {
            var direction = (target - Position).Normalize();
            var step = direction * CurrentSpeed * deltaTime;
            var newPosition = Position + step;

            if (Vector2Float.DistanceSQRT(Position, newPosition) >= Vector2Float.DistanceSQRT(Position, target))
            {
                newPosition = target;
                AdvanceToNextPoint();
            }

            Position = newPosition;
        }

        private void UpdateSpeed(double deltaTime)
        {
            if (IsMoving)
            {
                CurrentSpeed = Math.Min(CurrentSpeed + AccelerationForce * deltaTime, MaxSpeed);
            }
            else
            {
                CurrentSpeed = 0;
            }
        }

        private void UpdateChunkPosition()
        {
            var newChunkPosition = Position.ToInt();
            if (CurChunkPosition == newChunkPosition) return;

            BeforeChunkUpdate?.Invoke();

            var map = Game.Map.GetArrayMap();

            if (CurChunkPosition.X >= 0 && CurChunkPosition.Y >= 0 &&
                CurChunkPosition.X < map.GetLength(0) && CurChunkPosition.Y < map.GetLength(1))
            {
                var currentCell = map[CurChunkPosition.X, CurChunkPosition.Y];
                currentCell?.UnitsInPoint?.Remove(this);
            }

            CurChunkPosition = newChunkPosition;

            if (CurChunkPosition.X >= 0 && CurChunkPosition.Y >= 0 &&
                CurChunkPosition.X < map.GetLength(0) && CurChunkPosition.Y < map.GetLength(1))
            {
                var newCell = map[CurChunkPosition.X, CurChunkPosition.Y];
                newCell?.UnitsInPoint?.Add(this);
            }

            AfterChunkUpdate?.Invoke();
        }

        public override void Update()
        {
            foreach (BaseAttackingPoint attackPoint in AttackingPoints)
            {
                attackPoint.Update();
            }

            MoveToTarget();
            UpdateChunkPosition();
            CheckAttackTarget();
        }

        public override void Destroy()
        {
            base.Destroy();
        }

        public override void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            if (Game?.Map != null && Game.Map.GetArrayMap() is var map &&
                CurChunkPosition.X >= 0 && CurChunkPosition.X < map.GetLength(0) &&
                CurChunkPosition.Y >= 0 && CurChunkPosition.Y < map.GetLength(1))
            {
                map[CurChunkPosition.X, CurChunkPosition.Y].UnitsInPoint.Remove(this);
            }

            foreach (var attackPoint in AttackingPoints)
            {
                (attackPoint as IDisposable)?.Dispose();
            }
        }
    }

}