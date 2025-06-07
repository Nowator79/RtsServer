using Microsoft.Extensions.Logging;
using RtsServer.App.Battle.Dto;
using RtsServer.App.Battle.MapBattle.ChunksType;
using RtsServer.App.Battle.Navigator;
using RtsServer.App.Battle.Units.AttackingPoint;
using RtsServer.App.NetWork.Tcp;
using RtsServer.App.NetWorkDto;
using RtsServer.App.NetWorkResponseSender;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RtsServer.App.Battle.Units
{
    public class Unit : IDisposable
    {
        private const double RotationThreshold = 5.0;
        private const double PositionThreshold = 0.05;
        private const double SpeedAdjustmentFactor = 1.0;
        private bool _disposed;

        // Идентификаторы и базовые свойства
        public int Id { get; private set; }
        public string Code { get; }
        public int PlayerOwner { get; }
        public Health Health { get; }

        // Позиция и движение
        public Vector2Float Position { get; protected set; }
        public Vector2Int CurChunkPosition { get; protected set; }
        public Vector2Int TargetPosition { get; protected set; }
        public double Rotation { get; protected set; }

        // Характеристики движения
        public double CurrentSpeed { get; protected set; }
        public double MaxSpeed { get; protected set; } = 1.0;
        public double AccelerationForce { get; protected set; }
        public bool IsMoving { get; protected set; }
        public double RotationSpeed { get; protected set; }

        // Навигация
        public HashSet<Vector2Int> PathRoute { get; private set; }
        protected INavigator Navigator { get; }

        // Атака
        public BaseAttackingPoint[] AttackingPoints { get; protected set; } = Array.Empty<BaseAttackingPoint>();

        // Контекст
        public Game Game { get; private set; }
        public event Action BeforeChunkUpdate;
        public event Action AfterChunkUpdate;
        
        private ILogger<Unit> _logger;

        public Unit(string code, Health health, Vector2Float position, int playerOwner)
        {
            Code = code ?? throw new ArgumentNullException(nameof(code));
            Health = health;
            Position = position;
            PlayerOwner = playerOwner;
            Navigator = new GroundUnitNavigator();
            Navigator.SetUnit(this);
            TargetPosition = position.ToInt();
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            _logger = loggerFactory.CreateLogger<Unit>();
            _logger.LogDebug(position.ToString());
        }

        public void Init(Game context)
        {
            Game = context ?? throw new ArgumentNullException(nameof(context));
            Navigator.SetMap(Game.Map);

            foreach (var attackPoint in AttackingPoints)
            {
                attackPoint.Init();
            }
        }

        public void SetId(int id) => Id = id;

        public Unit SetTargetPosition(Vector2Int targetPosition)
        {
            TargetPosition = targetPosition;
            try
            {
                Navigator.Start();
            }
            catch (Exception ex)
            {
                // Логирование ошибки навигации
                _logger?.LogError(ex, "Navigation error for unit {UnitId}", Id);
            }
            return this;
        }

        public Unit SetAttackTarget(Unit targetUnit)
        {
            if (targetUnit == null) return this;

            foreach (var attackPoint in AttackingPoints)
            {
                attackPoint.SetTarget(targetUnit);
            }
            return this;
        }

        public async Task UpdatePathRouteAsync(HashSet<Vector2Int> route)
        {
            PathRoute = route ?? throw new ArgumentNullException(nameof(route));

            if (Game?.Players == null) return;

            var tasks = new List<Task>();
            foreach (var player in Game.Players.Where(p => p.UserAuth != null))
            {
                var userTcp = Game.BattleManager.GameServer.TcpServer.GetClientByUserAuth(player.UserAuth);
                if (userTcp != null)
                {
                    var sender = new UnitPathNavSender(userTcp);
                    tasks.Add(sender.SetDate(new NUnitPathNav(Id, route)).SendAsync());
                }
            }

            await Task.WhenAll(tasks);
        }

        /**
         * Движения вращения и прочая логика
         */
        protected virtual void MoveToTarget()
        {
            if (PathRoute == null || PathRoute.Count == 0)
            {
                IsMoving = false;
                CurrentSpeed = 0;
                return;
            }

            IsMoving = true;
            var deltaTime = Game.TimeSystem.GetDelta();
            var lastPoint = PathRoute.Last();
            var targetPosition = lastPoint.GetFloat();

            if (Vector2Float.Distance(Position, targetPosition) < PositionThreshold)
            {
                Position = targetPosition;
                PathRoute.Remove(lastPoint);
                return;
            }

            if (RotateTowards(targetPosition))
            {
                var direction = (targetPosition - Position).Normalize();
                var newPosition = Position + direction * CurrentSpeed * deltaTime;

                if (Vector2Float.DistanceSQRT(Position, newPosition) >= Vector2Float.DistanceSQRT(Position, targetPosition))
                {
                    newPosition = targetPosition;
                }

                Position = newPosition;
            }

            UpdateSpeed(deltaTime);
        }

        private bool RotateTowards(Vector2Float target)
        {
            var globalFacing = Vector2Float.VectorByAngle(-Rotation).Normalize();
            var angleToTarget = Vector2Float.AngleByVecotrs(globalFacing, (target - Position).Normalize());
            var rotationDirection = Math.Sign(Vector2Float.Cross(globalFacing, (target - Position).Normalize()));

            var deltaTime = Game.TimeSystem.GetDelta();
            var rotationStep = RotationSpeed * SpeedAdjustmentFactor * deltaTime;

            Rotation += -rotationDirection * Math.Min(rotationStep, angleToTarget);

            return angleToTarget % 180 < RotationThreshold || double.IsNaN(angleToTarget);
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
            map[CurChunkPosition.X, CurChunkPosition.Y].UnitsInPoint.Remove(this);
            CurChunkPosition = newChunkPosition;
            map[CurChunkPosition.X, CurChunkPosition.Y].UnitsInPoint.Add(this);

            AfterChunkUpdate?.Invoke();
        }

        public void Update()
        {
            foreach (var attackPoint in AttackingPoints)
            {
                attackPoint.Update();
            }

            MoveToTarget();
            UpdateChunkPosition();
        }

        public void Dispose()
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