using Microsoft.Extensions.Logging;
using RtsServer.App.Battle.Constructions;
using RtsServer.App.Battle.Dto;
using RtsServer.App.Battle.Interfaces;
using RtsServer.App.Battle.MapBattle;
using RtsServer.App.Battle.Navigator;
using RtsServer.App.Battle.Units.AttackingPoint;
using RtsServer.App.NetWork.Tcp;
using RtsServer.App.NetWorkDto;
using RtsServer.App.NetWorkResponseSender;

namespace RtsServer.App.Battle.Units
{
    public abstract class Unit : BattleEntity, IAttackTarget
    {
        private const double RotationThreshold = 5.0;
        private const double PositionThreshold = 0.05;
        private const double SpeedAdjustmentFactor = 2.0;

        /// <summary>Занимает клетку для наземной навигации (самолёты — нет).</summary>
        public virtual bool OccupiesGroundCell => true;

        /// <summary>Радиус круглой коллизии. Техника: диаметр 0.5. Пехота переопределяет радиус на 0.1.</summary>
        public virtual double CollisionRadius => OccupiesGroundCell ? 0.25 : 0;

        /// <summary>Домен цели для ПВО/наземного оружия.</summary>
        public virtual AttackTargetDomain AttackDomain => AttackTargetDomain.Ground;
        /// <summary>По умолчанию средняя броня; пехота/танки переопределяют.</summary>
        public virtual ArmorType Armor => ArmorType.Medium;

        public Health Health { get; }
        public Vector2Int CurChunkPosition { get; protected set; }
        public Vector2Int TargetPosition { get; protected set; }
        public double Rotation { get; protected set; }

        public double CurrentSpeed { get; protected set; }
        public double MaxSpeed { get; protected set; } = 1.0;
        public double AccelerationForce { get; protected set; }
        public bool IsMoving { get; protected set; }
        public double RotationSpeed { get; protected set; }

        /// <summary>Юнит стоит без активного маршрута — клетка занята насовсем.</summary>
        public bool IsParked =>
            !IsMoving && !targetPoint.HasValue && (PathRoute == null || PathRoute.Count == 0);

        /// <summary>Высота рельефа под юнитом (world Y локальной карты).</summary>
        public virtual double GetGroundElevation() => TerrainHeights.SampleAt(Game, Position);

        /// <summary>Высота корпуса юнита (для попадания по нему).</summary>
        public virtual double GetBodyHeight() => GetGroundElevation() + 0.5;

        public Vector2Float AimPosition => Position;

        /// <summary>Высота точки выстрела (дуло / подвеска).</summary>
        public virtual double GetMuzzleHeight() => GetGroundElevation() + 0.9;

        /// <summary>
        /// Автозахват врагов в радиусе. По умолчанию вкл (стоячие юниты отвечают на угрозу).
        /// Обычный ход временно выключает; attack-move включает на маршруте.
        /// </summary>
        public bool CanAutoAcquireTargets { get; protected set; } = true;

        /// <summary>Приказ «идти и атаковать» — постоянно переключаемся на ближайшую цель.</summary>
        public bool IsAttackMoving { get; protected set; }

        /// <summary>
        /// Радиус idle-агро: в покое ищем врага и едем/летим атаковать.
        /// По умолчанию — дальность оружия + запас.
        /// </summary>
        public virtual double IdleAggroRadius => GetMaxWeaponRange() + 8.0;

        public void SetId(int id) => Id = id;
        public Queue<Vector2Int> PathRoute { get; private set; }
        protected INavigator Navigator { get; }

        /// <summary>Клетка выезда с завода — применяется в Init после появления.</summary>
        public Vector2Int? PendingRallyCell { get; set; }

        public BaseAttackingPoint[] AttackingPoints { get; protected set; } = Array.Empty<BaseAttackingPoint>();
        public event Action BeforeChunkUpdate;
        public event Action AfterChunkUpdate;

        private Vector2Int? targetPoint;
        /// <summary>Обычный ход: до прибытия не стреляем, после — снова можно.</summary>
        private bool _holdFireUntilIdle;
        /// <summary>Конечная точка attack-move (куда возвращаемся после боя).</summary>
        private Vector2Int? _attackMoveGoal;
        /// <summary>Текущая цель преследования на attack-move.</summary>
        private IAttackTarget? _engageTarget;
        private Vector2Int? _chaseCell;
        /// <summary>Стоим и стреляем — путь не сбрасываем.</summary>
        private bool _holdingForAttack;
        /// <summary>Враг в радиусе — доезжаем до центра клетки, потом держим огонь.</summary>
        private bool _holdAfterWaypoint;
        /// <summary>Свежий attack-move: сначала едем, не встаём в той же клетке.</summary>
        private bool _attackMoveGrace;
        private Vector2Int _attackMoveGraceCell;
        private double _attackMoveGraceUntil;
        /// <summary>Прошлая клетка — не шагаем назад (анти-осцилляция толпы).</summary>
        private Vector2Int? _prevCell;
        private readonly Vector2Int[] _recentCells = new Vector2Int[8];
        private int _recentCount;
        private Vector2Int _stuckCell;
        private int _stuckTicks;
        private int _bodyBlockedTicks;
        /// <summary>Следующий A* считает чужие тела стенами. Включать только на один поиск.</summary>
        internal bool NavBlockBodies;
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

            if (PendingRallyCell is Vector2Int rally)
            {
                PendingRallyCell = null;
                SetTargetPosition(rally, attackMove: false);
            }
        }

        public virtual Unit SetTargetPosition(Vector2Int targetPosition, bool attackMove = false) =>
            SetTargetPosition(targetPosition, attackMove, reservations: null);

        public virtual Unit SetTargetPosition(
            Vector2Int targetPosition,
            bool attackMove,
            PathReservationMap? reservations)
        {
            if (targetPosition.X < 0 || targetPosition.Y < 0) return this;

            TargetPosition = targetPosition;
            IsAttackMoving = attackMove;
            CanAutoAcquireTargets = true;
            _holdFireUntilIdle = false;
            _holdingForAttack = false;
            _holdAfterWaypoint = false;
            _chaseCell = null;
            _prevCell = null;

            if (attackMove)
            {
                _attackMoveGoal = targetPosition;
                _engageTarget = null;
                BeginAttackMoveGrace();
            }
            else
            {
                _attackMoveGoal = null;
                _engageTarget = null;
                _attackMoveGrace = false;
            }

            // Цель атаки не сбрасываем на обычном ходе — пушка может вести огонь по дороге.
            // Attack-move цели ведёт UpdateAttackMoveEngagement.

            try
            {
                Navigator.Start(reservations);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Navigation error for unit {UnitId}", Id);
            }
            return this;
        }

        /// <summary>
        /// Групповой ход: финиш + уже готовый маршрут (spine + offset), без своего A*.
        /// </summary>
        public virtual Unit ApplyPreparedRoute(
            Vector2Int destination,
            IEnumerable<Vector2Int> routeCells,
            bool attackMove = false)
        {
            if (destination.X < 0 || destination.Y < 0) return this;

            TargetPosition = destination;
            IsAttackMoving = attackMove;
            CanAutoAcquireTargets = true;
            _holdFireUntilIdle = false;
            _holdingForAttack = false;
            _holdAfterWaypoint = false;
            _chaseCell = null;
            _prevCell = null;

            if (attackMove)
            {
                _attackMoveGoal = destination;
                _engageTarget = null;
                BeginAttackMoveGrace();
            }
            else
            {
                _attackMoveGoal = null;
                _engageTarget = null;
                _attackMoveGrace = false;
            }

            Vector2Int here = GetPathStartCell();
            List<Vector2Int> cells = routeCells as List<Vector2Int> ?? routeCells.ToList();
            if (cells.Count == 0 || !cells[^1].Equals(destination))
                cells.Add(destination);
            cells = GridPathUtil.EnsureAdjacent(cells);
            if (Game?.Map != null)
                cells = PathSmoother.Smooth(cells, Game.Map, Game);

            var queue = new Queue<Vector2Int>();
            Vector2Int? last = null;
            foreach (Vector2Int cell in cells)
            {
                if (last == null && cell.Equals(here))
                    continue;
                if (last.HasValue && last.Value.Equals(cell))
                    continue;
                queue.Enqueue(cell);
                last = cell;
            }

            SetPathRoute(queue);

            return this;
        }

        public virtual Unit SetAttackTarget(IAttackTarget target)
        {
            if (target == null) return this;

            // Явная атака по клику — держим выбранную цель, без переключения на ближайшую.
            IsAttackMoving = false;
            CanAutoAcquireTargets = true;
            _holdFireUntilIdle = false;
            _attackMoveGoal = null;
            _engageTarget = target;
            _chaseCell = null;
            _holdingForAttack = false;

            ApplyAttackTargetToTurrets(target);
            return this;
        }

        /// <summary>Назначить цель турелям без сброса флага attack-move.</summary>
        protected void ApplyAttackTargetToTurrets(IAttackTarget target)
        {
            foreach (var attackingPoint in AttackingPoints)
            {
                if (!attackingPoint.CanEngage(target))
                {
                    attackingPoint.ClearTarget();
                    continue;
                }
                attackingPoint.SetTarget(target);
            }
        }

        public void CheckAttackTarget()
        {
            foreach (BaseAttackingPoint attackingPoint in AttackingPoints)
            {
                attackingPoint.CheckTarget();
            }
        }

        /// <summary>
        /// Attack-move: идём к точке → нашли врага → подходим → в радиусе стоим и бьём →
        /// уехал далеко — догоняем → убили — снова к точке приказа.
        /// </summary>
        protected virtual void TryRetargetNearestOnAttackMove()
        {
            if (!IsAttackMoving || !CanAutoAcquireTargets || Game == null)
                return;
            // Воздух обрабатывает сам.
            if (!OccupiesGroundCell)
                return;

            UpdateGroundAttackMoveEngagement();
        }

        private void UpdateGroundAttackMoveEngagement()
        {
            double weaponRange = GetMaxWeaponRange();
            if (weaponRange <= 0)
                return;

            double huntRange = Math.Max(weaponRange + 12.0, IdleAggroRadius);
            const double chaseLeashBonus = 10.0;
            const double stopRangeFactor = 0.92;
            const double resumeChaseFactor = 1.05;

            if (_engageTarget != null && (_engageTarget.IsDestroyed || !Game.CanSee(OwnerId, _engageTarget.AimPosition)))
            {
                _engageTarget = null;
                _chaseCell = null;
                _holdingForAttack = false;
                _holdAfterWaypoint = false;
                ClearTurretTargets();
            }

            IAttackTarget? nearest = FindNearestEngageableEnemy(huntRange);

            if (_engageTarget != null)
            {
                double dEngage = Vector2Float.Distance(Position, _engageTarget.AimPosition);
                if (dEngage > huntRange + chaseLeashBonus)
                {
                    _engageTarget = null;
                    _chaseCell = null;
                    _holdingForAttack = false;
                    _holdAfterWaypoint = false;
                    ClearTurretTargets();
                }
                else if (nearest != null && !ReferenceEquals(nearest, _engageTarget))
                {
                    double dNear = Vector2Float.Distance(Position, nearest.AimPosition);
                    if (dNear < dEngage - 0.75)
                        _engageTarget = nearest;
                }
            }
            else if (nearest != null)
            {
                _engageTarget = nearest;
            }

            if (_engageTarget == null)
            {
                _holdingForAttack = false;
                _holdAfterWaypoint = false;
                _chaseCell = null;
                ClearTurretTargets();
                ResumeAttackMoveGoalIfNeeded();
                return;
            }

            double dist = Vector2Float.Distance(Position, _engageTarget.AimPosition);
            double stopAt = weaponRange * stopRangeFactor;
            double resumeMarch = weaponRange * resumeChaseFactor;

            // Уже держим клетку и бьём — пока цель в радиусе, стоим. Уехала — снова к точке приказа.
            if (_holdingForAttack)
            {
                if (dist <= resumeMarch)
                {
                    ApplyAttackTargetToTurrets(_engageTarget);
                    return;
                }

                _holdingForAttack = false;
                ResumeAttackMoveGoalIfNeeded();
                return;
            }

            if (dist <= stopAt)
            {
                ApplyAttackTargetToTurrets(_engageTarget);
                if (InAttackMoveGrace())
                {
                    _holdAfterWaypoint = false;
                    return;
                }

                // AoW3 assault: доезжаем до центра клетки, потом стоим. Не на полклетки.
                if (targetPoint.HasValue)
                {
                    double toCell = Vector2Float.Distance(Position, targetPoint.Value.GetFloat());
                    if (toCell >= PositionThreshold)
                    {
                        _holdAfterWaypoint = true;
                        return;
                    }
                }

                EnterAttackHold();
                return;
            }

            // Вне радиуса — идём к точке приказа (не срываем строй погоней за врагом).
            _holdAfterWaypoint = false;
            ClearTurretTargets();
            ResumeAttackMoveGoalIfNeeded();
        }

        private void EnterAttackHold()
        {
            _holdAfterWaypoint = false;
            _holdingForAttack = true;
            CurrentSpeed = 0;
            IsMoving = false;

            if (targetPoint.HasValue)
                Position = targetPoint.Value.GetFloat();
            else
                Position = Position.ToInt().GetFloat();

            targetPoint = null;
            // PathRoute и _attackMoveGoal остаются — после боя продолжим.
            if (_attackMoveGoal is Vector2Int goal)
                TargetPosition = goal;
        }

        private void ClearTurretTargets()
        {
            foreach (BaseAttackingPoint point in AttackingPoints)
                point.ClearTarget();
        }

        private void ChaseEngageTarget(IAttackTarget target)
        {
            Vector2Int cell = target.AimPosition.ToInt();
            if (cell.X < 0 || cell.Y < 0)
                return;

            // Не спамим pathfinding каждый тик — только если цель сменила клетку / стоим без пути.
            bool needPath =
                !_chaseCell.HasValue
                || Math.Abs(_chaseCell.Value.X - cell.X) + Math.Abs(_chaseCell.Value.Y - cell.Y) >= 2
                || IsParked
                || ((PathRoute == null || PathRoute.Count == 0) && !targetPoint.HasValue);

            if (!needPath)
                return;

            _chaseCell = cell;
            // Path к врагу, не сбрасывая attack-move / цель приказа.
            TargetPosition = cell;
            try
            {
                Navigator.StartRepath();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Chase repath failed for unit {UnitId}", Id);
            }

            // Погоня не построилась и идти некуда — не замираем, возвращаемся к точке приказа.
            if (!HasActiveRoute)
            {
                _engageTarget = null;
                _chaseCell = null;
                _holdingForAttack = false;
                ResumeAttackMoveGoalIfNeeded();
            }
        }

        private void ResumeAttackMoveGoalIfNeeded()
        {
            if (_attackMoveGoal is not Vector2Int goal)
                return;

            // Уже едем к цели приказа или стоим на ней.
            if (TargetPosition.Equals(goal) && (targetPoint.HasValue || (PathRoute != null && PathRoute.Count > 0)))
                return;

            Vector2Int here = Position.ToInt();
            if (here.Equals(goal) && IsParked)
                return;

            TargetPosition = goal;
            try
            {
                Navigator.Start();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Resume attack-move failed for unit {UnitId}", Id);
            }
        }

        protected IAttackTarget? GetCurrentTurretTarget()
        {
            foreach (BaseAttackingPoint point in AttackingPoints)
            {
                if (point.Target != null && !point.Target.IsDestroyed)
                    return point.Target;
            }
            return null;
        }

        protected double GetMaxWeaponRange()
        {
            double max = 0;
            foreach (BaseAttackingPoint point in AttackingPoints)
                max = Math.Max(max, point.Range);
            return max;
        }

        protected bool HasAnyTurretTarget()
        {
            foreach (BaseAttackingPoint point in AttackingPoints)
            {
                if (point.Target != null && !point.Target.IsDestroyed)
                    return true;
            }
            return false;
        }

        /// <summary>Покой: стоячий наземный / переопределяется у воздуха.</summary>
        protected virtual bool IsIdleForAggro() => IsParked;

        /// <summary>
        /// В покое: враг в IdleAggroRadius → взять в цель и поехать attack-move.
        /// </summary>
        protected virtual void TryIdleAggro()
        {
            if (Game == null || !CanAutoAcquireTargets)
                return;
            if (!IsIdleForAggro())
                return;
            // Уже стреляем по кому-то в радиусе оружия — стоим.
            if (HasAnyTurretTarget())
                return;

            IAttackTarget? enemy = FindNearestEngageableEnemy(IdleAggroRadius);
            if (enemy == null)
                return;

            SetAttackTarget(enemy);

            Vector2Int cell = enemy.AimPosition.ToInt();
            if (cell.X < 0 || cell.Y < 0)
                return;

            // Уже в клетке цели и в радиусе оружия — CheckTarget подхватит.
            if (Position.ToInt().Equals(cell))
                return;

            SetTargetPosition(cell, attackMove: true);
        }

        /// <summary>Ближайший враг/здание, которое может взять хотя бы одна турель.</summary>
        protected IAttackTarget? FindNearestEngageableEnemy(double range)
        {
            if (Game == null || range <= 0)
                return null;

            IAttackTarget? best = null;
            double bestDist = range;

            bool CanAnyTurretEngage(IAttackTarget t)
            {
                foreach (BaseAttackingPoint point in AttackingPoints)
                {
                    if (point.CanEngage(t))
                        return true;
                }
                return false;
            }

            foreach (Unit other in Game.Units)
            {
                if (other == null || other == this || other.IsDestroyed) continue;
                if (other.OwnerId == OwnerId) continue;
                if (!Game.CanSee(OwnerId, other.AimPosition)) continue;
                if (!CanAnyTurretEngage(other)) continue;
                double dist = Vector2Float.Distance(Position, other.AimPosition);
                if (dist >= bestDist) continue;
                bestDist = dist;
                best = other;
            }

            foreach (Construction construction in Game.Constructions)
            {
                if (construction == null || construction.IsDestroyed) continue;
                if (construction.OwnerId == OwnerId) continue;
                if (!Game.CanSee(OwnerId, construction.AimPosition)) continue;
                if (!CanAnyTurretEngage(construction)) continue;
                double dist = Vector2Float.Distance(Position, construction.AimPosition);
                if (dist >= bestDist) continue;
                bestDist = dist;
                best = construction;
            }

            return best;
        }

        /// <summary>Есть куда ехать: текущая клетка шага или хвост маршрута.</summary>
        internal bool HasActiveRoute =>
            targetPoint.HasValue || (PathRoute != null && PathRoute.Count > 0);

        /// <summary>Старт нового маршрута — клетка, на которой юнит стоит сейчас, а не старая точка пути.</summary>
        internal Vector2Int GetPathStartCell() => Position.ToInt();

        private void BeginAttackMoveGrace()
        {
            _attackMoveGrace = true;
            _attackMoveGraceCell = Position.ToInt();
            _attackMoveGraceUntil = Game != null ? Game.TimeSystem.GetTime() + 0.8 : 0;
        }

        /// <summary>Только что дали attack-move — не замираем в той же клетке.</summary>
        private bool InAttackMoveGrace()
        {
            if (!_attackMoveGrace || Game == null)
                return false;
            if (!Position.ToInt().Equals(_attackMoveGraceCell) || Game.TimeSystem.GetTime() >= _attackMoveGraceUntil)
            {
                _attackMoveGrace = false;
                return false;
            }
            return true;
        }

        /// <summary>
        /// Текущая промежуточная точка маршрута (куда юнит едет сейчас), если есть.
        /// </summary>
        internal Vector2Int? GetCurrentWaypoint() => targetPoint;

        /// <summary>Навигатор подставляет ближайшую свободную клетку вместо занятой цели.</summary>
        internal void SetNavigatingDestination(Vector2Int destination)
        {
            TargetPosition = destination;
        }

        /// <summary>
        /// Устанавливает новый маршрут. Сбрасывает текущую точку — юнит возьмёт первую точку из очереди.
        /// </summary>
        public void SetPathRoute(Queue<Vector2Int> route)
        {
            PathRoute = route ?? throw new ArgumentNullException(nameof(route));
            targetPoint = null;
        }

        /// <summary>
        /// Новый маршрут без сброса текущей промежуточной клетки — юнит доезжает, потом едет дальше.
        /// </summary>
        public void SetPathRouteKeepingCurrentWaypoint(Queue<Vector2Int> route)
        {
            if (route == null)
                throw new ArgumentNullException(nameof(route));

            if (targetPoint.HasValue && route.Count > 0 && route.Peek().Equals(targetPoint.Value))
                route.Dequeue();

            PathRoute = route;
            // targetPoint не трогаем.
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
            // Attack-move: в радиусе огня стоим и бьём, путь сохраняем.
            if (_holdingForAttack && IsAttackMoving)
            {
                CurrentSpeed = 0;
                IsMoving = false;
                return;
            }

            // Финиш — окрестность точки, не центр клетки. Иначе толпа снова стягивается в одну точку.
            bool routeLeft = PathRoute != null && PathRoute.Count > 0;
            bool lastLeg = !routeLeft && (!targetPoint.HasValue || targetPoint.Value.Equals(TargetPosition));
            if (OccupiesGroundCell
                && lastLeg
                && Vector2Float.Distance(Position, TargetPosition.GetFloat()) <= Math.Max(PositionThreshold, CollisionRadius * 2.05))
            {
                targetPoint = null;
                PathRoute?.Clear();
                StopMoving();
                return;
            }

            if (!HasNextTargetPoint())
            {
                StopMoving();
                return;
            }

            BreakDiagonalWaypoint();

            // Тела друг друга не блокируют клетку: расхождение делает круглый толчок.
            // Постройка на следующей клетке (или построили на маршруте) — сразу перестраиваем путь.
            if (targetPoint.HasValue && NavHelper.IsBlockedByConstruction(Game, targetPoint.Value))
            {
                CurrentSpeed = 0;
                targetPoint = null;
                try { Navigator.StartRepath(); }
                catch (Exception ex) { _logger?.LogError(ex, "Construction repath failed for unit {UnitId}", Id); }
                if (!HasNextTargetPoint())
                    return;
            }

            if (!targetPoint.HasValue)
            {
                CurrentSpeed = 0;
                IsMoving = false;
                return;
            }

            double deltaTime = Game.TimeSystem.GetDelta();
            Vector2Float targetPosition = GetCurrentTargetPosition();
            double distanceToTarget = Vector2Float.Distance(Position, targetPosition);

            if (IsReachedTarget(distanceToTarget))
            {
                SnapToTarget(targetPosition);
                AdvanceToNextPoint();
                if (_holdAfterWaypoint && IsAttackMoving)
                    EnterAttackHold();
                return;
            }

            IsMoving = true;

            // Сначала разворачиваемся к цели. Пока смотрим не туда — стоим и крутимся на месте.
            if (!RotateTowardsTarget(targetPosition, deltaTime))
            {
                CurrentSpeed = 0;
                return;
            }

            UpdateSpeed(deltaTime);
            MoveForwardTowards(targetPosition, deltaTime);
        }

        /// <summary>
        /// Локальный обход (SC / AoW3): путь стабильный, конфликт — шаг только ближе к слоту.
        /// Без полного A* на каждый затык и без «жду пока проедет».
        /// </summary>
        private bool HandleTrafficOnNextTile()
        {
            Vector2Int here = Position.ToInt();
            if (!here.Equals(_stuckCell))
            {
                _stuckCell = here;
                _stuckTicks = 0;
            }

            if (!targetPoint.HasValue || !IsCellBlockedForStep(targetPoint.Value))
                return false;

            Vector2Int blocked = targetPoint.Value;

            if (IsParkedOccupant(blocked) && (_stuckTicks == 0 || _stuckTicks >= 40))
            {
                RepathAroundBodies(blockMoving: false);
                _stuckTicks = 15;
                if (!targetPoint.HasValue)
                    HasNextTargetPoint();
                if (!targetPoint.HasValue)
                {
                    CurrentSpeed = 0;
                    IsMoving = false;
                    return true;
                }
                if (!IsCellBlockedForStep(targetPoint.Value))
                    return false;
            }
            else if (TryYieldToLowerId(here, blocked))
            {
                _stuckTicks = 0;
                return false;
            }

            _stuckTicks++;
            CurrentSpeed = 0;
            IsMoving = false;
            return true;
        }

        /// <summary>Нас просит клетка юнит с меньшим id — сходим в свободного соседа, маршрут сохраняем.</summary>
        private bool TryYieldToLowerId(Vector2Int here, Vector2Int blocked)
        {
            if (Game?.Units == null || Game.Map == null)
                return false;

            bool wanted = false;
            foreach (Unit other in Game.Units)
            {
                if (other == null || other == this || other.IsDestroyed || !other.OccupiesGroundCell)
                    continue;
                Vector2Int? step = other.GetCurrentWaypoint();
                if (step.HasValue && step.Value.Equals(here) && other.Id < Id)
                {
                    wanted = true;
                    break;
                }
            }
            if (!wanted)
                return false;

            int hereScore = ChebyshevNav(here, TargetPosition);
            Vector2Int? best = null;
            int bestScore = int.MaxValue;
            foreach (Vector2Int near in NavHelper.GetSafeNear(here, Game.Map.Width, Game.Map.Length))
            {
                if (near.Equals(blocked) || near.Equals(here) || IsRecentCell(near))
                    continue;
                if (!IsCellFreeToEnter(near))
                    continue;
                int score = ChebyshevNav(near, TargetPosition);
                if (score > hereScore + 1 || score >= bestScore)
                    continue;
                bestScore = score;
                best = near;
            }
            if (!best.HasValue)
                return false;

            var kept = new Queue<Vector2Int>();
            kept.Enqueue(blocked);
            Vector2Int tail = blocked;
            if (PathRoute != null)
            {
                while (PathRoute.Count > 0)
                {
                    Vector2Int cell = PathRoute.Dequeue();
                    if (cell.Equals(tail))
                        continue;
                    kept.Enqueue(cell);
                    tail = cell;
                }
            }
            RememberCell(here);
            PathRoute = kept;
            targetPoint = best;
            return true;
        }

        /// <summary>Обход толпы одним поиском: чужие тела — стены, старт с текущей клетки.</summary>
        private void RepathAroundBodies(bool blockMoving)
        {
            _stuckTicks = 0;
            Vector2Int? savedPoint = targetPoint;
            Queue<Vector2Int> savedRoute = PathRoute == null
                ? new Queue<Vector2Int>()
                : new Queue<Vector2Int>(PathRoute);

            targetPoint = null;
            PathRoute = new Queue<Vector2Int>();
            NavBlockBodies = blockMoving;
            try
            {
                Navigator.StartRepath();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Body repath failed for unit {UnitId}", Id);
            }
            finally
            {
                NavBlockBodies = false;
            }

            if (!HasActiveRoute)
            {
                targetPoint = savedPoint;
                PathRoute = savedRoute;
            }
        }

        /// <summary>
        /// Шаг вплотную к занятой клетке маршрута и не дальше от своего слота.
        /// Занятую клетку возвращаем в голову очереди.
        /// </summary>
        private bool TryStepAside(Vector2Int here, Vector2Int blocked)
        {
            if (Game?.Map == null)
                return false;

            Vector2Int anchor = TargetPosition;
            if (PathRoute != null)
            {
                foreach (Vector2Int cell in PathRoute)
                {
                    if (!cell.Equals(blocked))
                    {
                        anchor = cell;
                        break;
                    }
                }
            }

            int blockedCheb = ChebyshevNav(blocked, anchor);
            int hereToSlot = ChebyshevNav(here, TargetPosition);
            Vector2Int? best = null;
            int bestScore = int.MaxValue;

            foreach (Vector2Int near in NavHelper.GetSafeNear(here, Game.Map.Width, Game.Map.Length))
            {
                if (near.Equals(blocked) || near.Equals(here))
                    continue;
                if (IsRecentCell(near))
                    continue;
                if (ChebyshevNav(near, blocked) > 1)
                    continue;
                if (ChebyshevNav(near, TargetPosition) > hereToSlot)
                    continue;
                if (!IsCellFreeToEnter(near))
                    continue;

                int score = ChebyshevNav(near, anchor);
                if (score > blockedCheb + 1)
                    continue;
                if (score >= bestScore)
                    continue;

                bestScore = score;
                best = near;
            }

            if (!best.HasValue)
                return false;

            var kept = new Queue<Vector2Int>();
            kept.Enqueue(blocked);
            Vector2Int tail = blocked;
            if (PathRoute != null)
            {
                while (PathRoute.Count > 0)
                {
                    Vector2Int cell = PathRoute.Dequeue();
                    if (cell.Equals(tail))
                        continue;
                    kept.Enqueue(cell);
                    tail = cell;
                }
            }

            RememberCell(here);
            PathRoute = kept;
            _prevCell = here;
            targetPoint = best;
            return true;
        }

        private bool IsRecentCell(Vector2Int cell)
        {
            int n = Math.Min(_recentCount, _recentCells.Length);
            for (int i = 0; i < n; i++)
            {
                if (_recentCells[i].Equals(cell))
                    return true;
            }
            return false;
        }

        private void RememberCell(Vector2Int cell)
        {
            if (_recentCount > 0 && _recentCells[(_recentCount - 1) % _recentCells.Length].Equals(cell))
                return;
            _recentCells[_recentCount % _recentCells.Length] = cell;
            _recentCount++;
        }

        private static int Dist2(Vector2Int a, Vector2Int b)
        {
            int dx = a.X - b.X;
            int dy = a.Y - b.Y;
            return dx * dx + dy * dy;
        }

        private bool IsCellBlockedForStep(Vector2Int cell) => !IsCellFreeToEnter(cell);

        /// <summary>Стоячего на клетке просят отойти, чтобы пропустили. Потом он вернётся в свой слот.</summary>
        internal bool NudgeOff(Vector2Int requesterCell)
        {
            if (!IsParked || Game?.Map == null)
                return false;

            Vector2Int here = Position.ToInt();
            Vector2Int? best = null;
            int bestDist = -1;
            foreach (Vector2Int near in NavHelper.GetSafeNear(here, Game.Map.Width, Game.Map.Length))
            {
                if (!IsCellFreeToEnter(near))
                    continue;
                int dist = ChebyshevNav(near, requesterCell);
                if (dist > bestDist)
                {
                    bestDist = dist;
                    best = near;
                }
            }
            if (!best.HasValue)
                return false;

            var back = new Queue<Vector2Int>();
            back.Enqueue(TargetPosition);
            PathRoute = back;
            targetPoint = best;
            IsMoving = true;
            return true;
        }

        private Unit? FindParkedOccupant(Vector2Int cell)
        {
            if (Game?.Map == null) return null;
            var tile = Game.Map.GetArrayMap()[cell.X, cell.Y];
            if (tile?.UnitsInPoint == null) return null;
            foreach (Unit other in tile.UnitsInPoint)
            {
                if (other == null || other == this || other.IsDestroyed || !other.OccupiesGroundCell)
                    continue;
                if (other.IsParked)
                    return other;
            }
            return null;
        }

        private bool IsParkedOccupant(Vector2Int cell) => FindParkedOccupant(cell) != null;

        /// <summary>
        /// Можно ли войти: рельеф, постройка, тело на клетке, чужой текущий шаг.
        /// Едущий, который уже уходит с клетки, клетку не держит.
        /// </summary>
        private bool IsCellFreeToEnter(Vector2Int cell)
        {
            var map = Game.Map.GetArrayMap();
            if (cell.X < 0 || cell.Y < 0 || cell.X >= map.GetLength(0) || cell.Y >= map.GetLength(1))
                return false;

            var tile = map[cell.X, cell.Y];
            if (!NavHelper.IsWalkableHeight(tile))
                return false;

            Vector2Int here = Position.ToInt();
            if (here.X >= 0 && here.Y >= 0 && here.X < map.GetLength(0) && here.Y < map.GetLength(1))
            {
                var from = map[here.X, here.Y];
                if (from != null && !NavHelper.CanStepHeight(from.Height, tile.Height))
                    return false;

                int dx = cell.X - here.X;
                int dy = cell.Y - here.Y;
                if (dx != 0 && dy != 0)
                {
                    int cx = here.X + dx;
                    int cy = here.Y + dy;
                    if (cx < 0 || cy < 0 || cx >= map.GetLength(0) || cy >= map.GetLength(1))
                        return false;
                    var cornerA = map[cx, here.Y];
                    var cornerB = map[here.X, cy];
                    if (cornerA == null || cornerB == null)
                        return false;
                    if (!NavHelper.CanStepHeight(from.Height, cornerA.Height)
                        || !NavHelper.CanStepHeight(from.Height, cornerB.Height))
                        return false;
                }
            }

            if (NavHelper.IsBlockedByConstruction(Game, cell))
                return false;

            if (tile.UnitsInPoint != null)
            {
                foreach (Unit other in tile.UnitsInPoint)
                {
                    if (other == null || other == this || other.IsDestroyed || !other.OccupiesGroundCell)
                        continue;
                    return false;
                }
            }

            if (Game?.Units == null)
                return true;

            int myDist = ChebyshevNav(Position.ToInt(), cell);
            foreach (Unit other in Game.Units)
            {
                if (other == null || other == this || other.IsDestroyed || !other.OccupiesGroundCell)
                    continue;
                Vector2Int? step = other.GetCurrentWaypoint();
                if (!step.HasValue || !step.Value.Equals(cell))
                    continue;
                int theirDist = ChebyshevNav(other.Position.ToInt(), cell);
                if (theirDist < myDist || (theirDist == myDist && other.Id < Id))
                    return false;
            }

            return true;
        }

        private static int ChebyshevNav(Vector2Int a, Vector2Int b) =>
            Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));

        /// <summary>
        /// Диагональ между центрами клеток проходит через угол. Шагаем только кардинально,
        /// иначе пол клетки оказывается на обрыве и юнит с него не сходит.
        /// </summary>
        private void BreakDiagonalWaypoint()
        {
            if (!targetPoint.HasValue || Game?.Map == null)
                return;

            Vector2Int here = Position.ToInt();
            Vector2Int tp = targetPoint.Value;
            int dx = Math.Sign(tp.X - here.X);
            int dy = Math.Sign(tp.Y - here.Y);
            if (dx == 0 || dy == 0)
                return;
            if (Math.Abs(tp.X - here.X) > 1 || Math.Abs(tp.Y - here.Y) > 1)
                return;

            var horizontal = new Vector2Int(here.X + dx, here.Y);
            var vertical = new Vector2Int(here.X, here.Y + dy);
            Vector2Int? step = null;
            if (IsCellFreeToEnter(horizontal))
                step = horizontal;
            else if (IsCellFreeToEnter(vertical))
                step = vertical;
            if (!step.HasValue)
                return;

            var kept = new Queue<Vector2Int>();
            kept.Enqueue(tp);
            if (PathRoute != null)
            {
                while (PathRoute.Count > 0)
                    kept.Enqueue(PathRoute.Dequeue());
            }
            PathRoute = kept;
            targetPoint = step;
        }

        private bool HasNextTargetPoint()
        {
            if (targetPoint.HasValue) return true;

            if (PathRoute != null && PathRoute.Count > 0)
            {
                _prevCell = Position.ToInt();
                targetPoint = PathRoute.Dequeue();
                return true;
            }

            return false;
        }

        private void StopMoving()
        {
            IsMoving = false;
            CurrentSpeed = 0;

            // Доехали после обычного хода — снова отвечаем, если враг подойдёт.
            if (_holdFireUntilIdle)
            {
                CanAutoAcquireTargets = true;
                _holdFireUntilIdle = false;
            }
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
            if (double.IsNaN(angleToTarget))
                return true;

            // Если цель ровно сзади, Cross == 0 — явно выбираем сторону разворота.
            var rotationDirection = Math.Sign(Vector2Float.Cross(globalFacing, toTarget));
            if (rotationDirection == 0 && angleToTarget > RotationThreshold)
                rotationDirection = 1;

            var rotationStep = RotationSpeed * SpeedAdjustmentFactor * deltaTime;
            Rotation += -rotationDirection * Math.Min(rotationStep, angleToTarget);

            // Без % 180: иначе угол ~180° ошибочно считался "выровненным" и техника ехала задом.
            return angleToTarget < RotationThreshold;
        }

        private void MoveForwardTowards(Vector2Float target, double deltaTime)
        {
            // Движемся только вперёд по курсу корпуса, не "скользим" к цели боком/назад.
            var forward = Vector2Float.VectorByAngle(-Rotation).Normalize();
            var step = forward * CurrentSpeed * deltaTime;
            var newPosition = Position + step;
            bool reached = Vector2Float.DistanceSQRT(Position, newPosition) >= Vector2Float.DistanceSQRT(Position, target);
            if (reached)
                newPosition = target;

            if (Game?.Map != null)
            {
                var tiles = Game.Map.GetArrayMap();
                newPosition.X = Math.Clamp(newPosition.X, 0, tiles.GetLength(0) - 0.001);
                newPosition.Y = Math.Clamp(newPosition.Y, 0, tiles.GetLength(1) - 0.001);
                if (!TryMoveBody(newPosition))
                {
                    CurrentSpeed = 0;
                    _bodyBlockedTicks++;
                    if (_bodyBlockedTicks >= 10)
                    {
                        _bodyBlockedTicks = 0;
                        try { Navigator.StartRepath(); }
                        catch (Exception ex) { _logger?.LogError(ex, "Corner repath failed for unit {UnitId}", Id); }
                    }
                    return;
                }

                _bodyBlockedTicks = 0;
            }
            else
            {
                Position = newPosition;
            }

            if (reached && Vector2Float.Distance(Position, target) <= PositionThreshold)
                AdvanceToNextPoint();
        }

        /// <summary>
        /// Сдвинуть тело, если круг не задевает воду, обрыв, скалу или постройку.
        /// Если целиком нельзя — скользим вдоль одной оси.
        /// </summary>
        internal bool TryMoveBody(Vector2Float desired)
        {
            if (!OccupiesGroundCell)
            {
                Position = desired;
                return true;
            }

            if (CanOccupy(desired))
            {
                Position = desired;
                UpdateChunkPosition();
                return true;
            }

            Vector2Float xOnly = new(desired.X, Position.Y);
            if (CanOccupy(xOnly))
            {
                Position = xOnly;
                UpdateChunkPosition();
                return true;
            }

            Vector2Float yOnly = new(Position.X, desired.Y);
            if (CanOccupy(yOnly))
            {
                Position = yOnly;
                UpdateChunkPosition();
                return true;
            }

            return false;
        }

        private bool CanOccupy(Vector2Float pos)
        {
            if (Game?.Map == null)
                return false;

            var tiles = Game.Map.GetArrayMap();
            int width = tiles.GetLength(0);
            int length = tiles.GetLength(1);
            Vector2Int stand = Position.ToInt();
            if (stand.X < 0 || stand.Y < 0 || stand.X >= width || stand.Y >= length)
                return false;

            var standTile = tiles[stand.X, stand.Y];
            if (standTile == null)
                return false;
            int standHeight = standTile.Height;
            return !NavHelper.CircleHitsBlocked(Game.Map, Game, pos.X, pos.Y, CollisionRadius, standHeight);
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

        protected void UpdateChunkPosition()
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

            // В клетку пишем всех (нужно для урона/взрывов).
            // OccupiesGroundCell=false у самолётов — только чтобы не блокировать наземный pathfinding.
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
            // Attack-move на земле сам ведёт цели (стоп/погоня/возврат к точке).
            if (!(IsAttackMoving && OccupiesGroundCell))
                CheckAttackTarget();

            TryRetargetNearestOnAttackMove();
            TryIdleAggro();

            foreach (BaseAttackingPoint attackPoint in AttackingPoints)
            {
                attackPoint.Update();
            }

            MoveToTarget();
            UpdateChunkPosition();
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