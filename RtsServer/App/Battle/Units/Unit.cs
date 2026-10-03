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
        private const double SpeedAdjustmentFactor = 1.0;
        /// <summary>Стоящий юнит не уедет — сразу строим объезд.</summary>
        private const int TrafficRepathParkedFrames = 2;
        /// <summary>Чужой/враг едет по клетке — подождать перед объездом.</summary>
        private const int TrafficRepathMovingFrames = 12;

        /// <summary>Занимает клетку для наземной навигации (самолёты — нет).</summary>
        public virtual bool OccupiesGroundCell => true;

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

        /// <summary>Высота корпуса юнита (для попадания по нему).</summary>
        public virtual double GetBodyHeight() => 0.5;

        public Vector2Float AimPosition => Position;

        /// <summary>Высота точки выстрела (дуло / подвеска).</summary>
        public virtual double GetMuzzleHeight() => 0.9;

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
        private int _trafficBlockedFrames;
        /// <summary>Обычный ход: до прибытия не стреляем, после — снова можно.</summary>
        private bool _holdFireUntilIdle;
        /// <summary>Конечная точка attack-move (куда возвращаемся после боя).</summary>
        private Vector2Int? _attackMoveGoal;
        /// <summary>Текущая цель преследования на attack-move.</summary>
        private IAttackTarget? _engageTarget;
        private Vector2Int? _chaseCell;
        /// <summary>Стоим и стреляем — путь не сбрасываем.</summary>
        private bool _holdingForAttack;
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
            _chaseCell = null;

            if (attackMove)
            {
                _attackMoveGoal = targetPosition;
                _engageTarget = null;
            }
            else
            {
                _attackMoveGoal = null;
                _engageTarget = null;
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
            _chaseCell = null;

            if (attackMove)
            {
                _attackMoveGoal = destination;
                _engageTarget = null;
            }
            else
            {
                _attackMoveGoal = null;
                _engageTarget = null;
            }

            Vector2Int here = GetPathStartCell();
            List<Vector2Int> cells = routeCells as List<Vector2Int> ?? routeCells.ToList();
            if (cells.Count == 0 || !cells[^1].Equals(destination))
                cells.Add(destination);
            cells = GridPathUtil.EnsureAdjacent(cells);

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

            if (_engageTarget != null && _engageTarget.IsDestroyed)
            {
                _engageTarget = null;
                _chaseCell = null;
                _holdingForAttack = false;
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
                _chaseCell = null;
                ClearTurretTargets();
                ResumeAttackMoveGoalIfNeeded();
                return;
            }

            double dist = Vector2Float.Distance(Position, _engageTarget.AimPosition);
            double stopAt = weaponRange * stopRangeFactor;
            double chaseAgainAt = weaponRange * resumeChaseFactor;

            if (_holdingForAttack)
            {
                // Уже стоим в огне: если цель отъехала — снова догоняем.
                if (dist <= chaseAgainAt)
                {
                    ApplyAttackTargetToTurrets(_engageTarget);
                    return;
                }

                _holdingForAttack = false;
            }

            if (dist <= stopAt)
            {
                ApplyAttackTargetToTurrets(_engageTarget);
                _holdingForAttack = true;
                CurrentSpeed = 0;
                IsMoving = false;
                return;
            }

            // Вне радиуса — турели молчат, едем к цели.
            ClearTurretTargets();
            _holdingForAttack = false;
            ChaseEngageTarget(_engageTarget);
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
                if (!CanAnyTurretEngage(construction)) continue;
                double dist = Vector2Float.Distance(Position, construction.AimPosition);
                if (dist >= bestDist) continue;
                bestDist = dist;
                best = construction;
            }

            return best;
        }

        /// <summary>
        /// Старт для построения маршрута: текущая точка движения (waypoint), если есть, иначе позиция юнита.
        /// </summary>
        internal Vector2Int GetPathStartCell() => targetPoint ?? Position.ToInt();

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
            _trafficBlockedFrames = 0;
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

            if (!HasNextTargetPoint())
            {
                _trafficBlockedFrames = 0;
                StopMoving();
                return;
            }

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

            if (HandleTrafficOnNextTile())
                return;

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
        /// Занятая следующая клетка: за союзником в колонне — просто ждём;
        /// объезд только если впереди стоячий / враг / долгий затык.
        /// </summary>
        /// <returns>true — этот кадр стоим.</returns>
        private bool HandleTrafficOnNextTile()
        {
            if (!targetPoint.HasValue || !IsNextTileOccupiedByOther())
            {
                _trafficBlockedFrames = 0;
                return false;
            }

            CurrentSpeed = 0;
            IsMoving = true;

            // Союзник едет впереди по тому же направлению — не разворачиваемся в бок.
            if (IsFriendlyMoverBlockingAhead())
            {
                _trafficBlockedFrames = 0;
                return true;
            }

            _trafficBlockedFrames++;
            int repathAfter = IsNextTileBlockedByParked()
                ? TrafficRepathParkedFrames
                : TrafficRepathMovingFrames;

            if (_trafficBlockedFrames < repathAfter)
                return true;

            _trafficBlockedFrames = 0;
            Vector2Int blocked = targetPoint.Value;
            targetPoint = null;
            try
            {
                // Не меняем финиш — иначе весь строй «расползается» вбок при каждой пробке.
                Navigator.StartRepath();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Traffic repath failed for unit {UnitId}", Id);
            }

            if (!HasNextTargetPoint() || IsNextTileOccupiedByOther())
            {
                targetPoint = null;
                if (!TrySidestepForward(blocked))
                    return true;
            }

            return false;
        }

        /// <summary>Впереди свой едущий юнит — колонна, ждём.</summary>
        private bool IsFriendlyMoverBlockingAhead()
        {
            if (!targetPoint.HasValue || Game?.Map == null)
                return false;

            var tile = Game.Map.GetArrayMap()[targetPoint.Value.X, targetPoint.Value.Y];
            if (tile?.UnitsInPoint == null)
                return false;

            bool sawFriendlyMover = false;
            foreach (Unit other in tile.UnitsInPoint)
            {
                if (other == null || other == this || other.IsDestroyed || !other.OccupiesGroundCell)
                    continue;

                if (other.OwnerId != OwnerId)
                    return false;

                if (other.IsParked)
                    return false;

                sawFriendlyMover = true;
            }

            return sawFriendlyMover;
        }

        /// <summary>Шаг только вперёд/вбок к цели, назад не уезжаем.</summary>
        private bool TrySidestepForward(Vector2Int blocked)
        {
            if (Game?.Map == null)
                return false;

            Vector2Int here = Position.ToInt();
            int hereDist = Dist2(here, TargetPosition);
            Vector2Int? best = null;
            int bestScore = int.MaxValue;

            foreach (Vector2Int near in NavHelper.GetSafeNear(here, Game.Map.Width, Game.Map.Length))
            {
                if (near.Equals(blocked) || !IsCellFree(near))
                    continue;

                int nearDist = Dist2(near, TargetPosition);
                // Не отъезжать от цели дальше, чем стоим сейчас.
                if (nearDist > hereDist)
                    continue;

                if (nearDist >= bestScore)
                    continue;

                bestScore = nearDist;
                best = near;
            }

            if (!best.HasValue)
                return false;

            targetPoint = best;
            while (PathRoute != null && PathRoute.Count > 0 && !IsCellFree(PathRoute.Peek()))
                PathRoute.Dequeue();

            return true;
        }

        private static int Dist2(Vector2Int a, Vector2Int b)
        {
            int dx = a.X - b.X;
            int dy = a.Y - b.Y;
            return dx * dx + dy * dy;
        }

        private bool IsCellFree(Vector2Int cell)
        {
            var map = Game.Map.GetArrayMap();
            if (cell.X < 0 || cell.Y < 0 || cell.X >= map.GetLength(0) || cell.Y >= map.GetLength(1))
                return false;

            var tile = map[cell.X, cell.Y];
            if (tile == null || tile.Height != 1 || tile.Id == 0)
                return false;

            if (NavHelper.IsBlockedByConstruction(Game, cell))
                return false;

            if (tile.UnitsInPoint == null)
                return true;

            foreach (Unit other in tile.UnitsInPoint)
            {
                if (other == null || other == this || other.IsDestroyed || !other.OccupiesGroundCell)
                    continue;
                return false;
            }

            return true;
        }

        private bool IsNextTileOccupiedByOther()
        {
            if (!targetPoint.HasValue || Game?.Map == null)
                return false;

            var tile = Game.Map.GetArrayMap()[targetPoint.Value.X, targetPoint.Value.Y];
            if (tile?.UnitsInPoint == null || tile.UnitsInPoint.Count == 0)
                return false;

            foreach (Unit other in tile.UnitsInPoint)
            {
                if (other == null || other == this || other.IsDestroyed || !other.OccupiesGroundCell)
                    continue;
                return true;
            }

            return false;
        }

        private bool IsNextTileBlockedByParked()
        {
            if (!targetPoint.HasValue || Game?.Map == null)
                return false;

            var tile = Game.Map.GetArrayMap()[targetPoint.Value.X, targetPoint.Value.Y];
            if (tile?.UnitsInPoint == null)
                return false;

            foreach (Unit other in tile.UnitsInPoint)
            {
                if (other == null || other == this || other.IsDestroyed || !other.OccupiesGroundCell)
                    continue;
                if (other.IsParked)
                    return true;
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
            _trafficBlockedFrames = 0;
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