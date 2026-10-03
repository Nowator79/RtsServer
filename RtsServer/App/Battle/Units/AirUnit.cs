using RtsServer.App.Battle.Constructions;
using RtsServer.App.Battle.Dto;
using RtsServer.App.Battle.Interfaces;
using RtsServer.App.Battle.Units.AttackingPoint;

namespace RtsServer.App.Battle.Units
{
    /// <summary>
    /// Самолёт атакует пролётами: заход → очередь → уход.
    /// Новый заход только после перезарядки магазина (4 ракеты / 5 с).
    /// </summary>
    public class AirUnit : Unit
    {
        private enum FlightMode
        {
            Cruise,      // полёт к точке приказа / по курсу
            Approach,    // заход на цель атаки
            Strafe,      // пролёт с огнём
            BreakAway    // уход после пролёта и разворот
        }

        private FlightMode _flightMode = FlightMode.Cruise;
        private Vector2Float _cruisePoint;
        private bool _hasCruisePoint;

        private IAttackTarget? _attackTarget;
        private int _shotsThisPass;
        private double _breakAwayTraveled;
        private Vector2Float _breakAwayDir;

        // Параметры боевого пролёта
        private const double StrafeEnterDistance = 16.0;
        private const double StrafeMaxAngle = 45.0;
        private const double PassCompleteDistance = 2.5;
        private const int ShotsPerPass = 4;
        private const double BreakAwayDistance = 16.0;
        private const double ReapproachDistance = 20.0;
        /// <summary>На attack-move ищем врага дальше оружия — чтобы сразу идти в заход.</summary>
        private const double AttackMoveHuntRange = 55.0;
        /// <summary>В покое (орбита у точки) — радиус idle-агро.</summary>
        private const double IdleHuntRange = 40.0;
        /// <summary>Радиус патрульного круга в пассиве у точки приказа.</summary>
        private const double OrbitRadius = 7.0;
        private const double OrbitLookAheadRadians = 0.95;

        public bool IsStrafing => _flightMode == FlightMode.Strafe;

        public override double IdleAggroRadius => IdleHuntRange;

        /// <summary>Базовая высота полёта; фактическая качается ±AltitudeBobAmplitude.</summary>
        public const double BaseFlightAltitude = 6.0;
        private const double AltitudeBobAmplitude = 2.0;
        /// <summary>Полный цикл подъём/спуск, рад/с.</summary>
        private const double AltitudeBobSpeed = 0.55;

        private double _altitudePhase;

        /// <summary>Текущая высота полёта над картой (с бобом).</summary>
        public double FlightAltitude { get; private set; } = BaseFlightAltitude;

        public override double GetBodyHeight() => FlightAltitude;
        public override double GetMuzzleHeight() => FlightAltitude;
        public override bool OccupiesGroundCell => false;
        public override AttackTargetDomain AttackDomain => AttackTargetDomain.Air;
        /// <summary>Лёгкий планер — уязвим к ПВО/AAM, пулемёты тоже кусают.</summary>
        public override ArmorType Armor => ArmorType.Light;

        /// <summary>Общий магазин AGM+AAM: 4 ракеты, затем перезарядка 5 с.</summary>
        private const int MagazineSize = 4;
        private const double MagazineReloadSeconds = 5.0;
        private int _rocketsLeft = MagazineSize;
        private double _reloadReadyAt;

        public AirUnit(Vector2Float position, int playerOwner)
            : base("AirUnit", 800, 800, position, playerOwner)
        {
            MaxSpeed = 6.4;
            AccelerationForce = 10.0;
            RotationSpeed = 110.0;
            // AGM — земля/здания, AAM — воздух. Домены не смешиваются.
            AttackingPoints = [new AgmAirCannon(this), new AamAirCannon(this)];
            // Самолёт без приказа не охотится сам — только attack-move / явная атака.
            CanAutoAcquireTargets = false;
            // Разный фазовый сдвиг, чтобы строй не «дышал» синхронно.
            _altitudePhase = (position.X * 0.37 + position.Y * 0.21) % (Math.PI * 2.0);
            UpdateFlightAltitude(0);
        }

        /// <summary>Можно ли стрелять (магазин готов).</summary>
        public bool CanFireRocket()
        {
            EnsureMagazineReady();
            return _rocketsLeft > 0;
        }

        /// <summary>Списать одну ракету из магазина. false — перезарядка / пусто.</summary>
        public bool TryConsumeRocket()
        {
            EnsureMagazineReady();
            if (_rocketsLeft <= 0)
                return false;

            _rocketsLeft--;
            if (_rocketsLeft <= 0 && Game != null)
                _reloadReadyAt = Game.TimeSystem.GetTime() + MagazineReloadSeconds;
            return true;
        }

        private void EnsureMagazineReady()
        {
            if (_rocketsLeft > 0 || Game == null)
                return;
            if (Game.TimeSystem.GetTime() >= _reloadReadyAt)
                _rocketsLeft = MagazineSize;
        }

        public override Unit SetTargetPosition(Vector2Int targetPosition, bool attackMove = false)
        {
            if (targetPosition.X < 0 || targetPosition.Y < 0) return this;

            TargetPosition = targetPosition;
            _cruisePoint = targetPosition.GetFloat();
            ClampCruisePointInsideMap();
            _hasCruisePoint = true;
            IsAttackMoving = attackMove;
            CanAutoAcquireTargets = attackMove;
            // Сброс текущего боевого пролёта; при attack-move сразу ищем цель.
            ClearAttackRun(clearWeaponTargets: true);

            // Только на старте (ещё не летим) — иначе резкий разворот игнорит RotationSpeed.
            if (CurrentSpeed < 0.2)
                FaceTowards(_cruisePoint);

            if (attackMove)
            {
                CheckAttackTarget();
                RefreshAttackTarget();
                if (_attackTarget == null)
                    TryHuntNearestEnemy(AttackMoveHuntRange);
            }

            return this;
        }

        public override void Update()
        {
            // Захват до руления — attack-move не ждёт лишний тик на круизе.
            CheckAttackTarget();
            RefreshAttackTarget();

            foreach (BaseAttackingPoint attackPoint in AttackingPoints)
                attackPoint.Update();

            MoveToTarget();
            // В клетках нужны для урона/взрывов; OccupiesGroundCell=false — навигация земли их игнорирует.
            UpdateChunkPosition();
        }

        public override Unit SetAttackTarget(IAttackTarget target)
        {
            if (target == null)
            {
                IsAttackMoving = false;
                CanAutoAcquireTargets = false;
                ClearAttackRun(clearWeaponTargets: true);
                return this;
            }

            // Фокус на выбранной цели, без автоохоты по дороге.
            IsAttackMoving = false;
            CanAutoAcquireTargets = false;

            bool anyTurret = false;
            foreach (BaseAttackingPoint attackingPoint in AttackingPoints)
            {
                if (!attackingPoint.CanEngage(target))
                {
                    attackingPoint.ClearTarget();
                    continue;
                }
                attackingPoint.SetTarget(target);
                anyTurret = true;
            }

            if (!anyTurret)
            {
                ClearAttackRun(clearWeaponTargets: true);
                return this;
            }

            _attackTarget = target;
            _shotsThisPass = 0;
            _breakAwayTraveled = 0;
            _flightMode = FlightMode.Approach;
            return this;
        }

        protected override void MoveToTarget()
        {
            double deltaTime = Game.TimeSystem.GetDelta();
            if (deltaTime <= 0)
                return;

            RefreshAttackTarget();

            Vector2Float desiredDir = GetForward2D();

            if (_attackTarget != null && !_attackTarget.IsDestroyed)
            {
                desiredDir = UpdateCombatFlight(deltaTime);
            }
            else
            {
                EnsureCruisePoint();
                desiredDir = UpdateCruiseFlight();
            }

            // Приоритетнее боя/круиза: не улетать за карту.
            desiredDir = ApplyMapBoundsSteering(desiredDir);

            RotateTowards(desiredDir, deltaTime);
            UpdateSpeed(deltaTime);
            FlyForward(deltaTime);
            UpdateFlightAltitude(deltaTime);
            ClampPositionToMap();
        }

        private void UpdateFlightAltitude(double deltaTime)
        {
            _altitudePhase += AltitudeBobSpeed * deltaTime;
            if (_altitudePhase > Math.PI * 2.0)
                _altitudePhase %= Math.PI * 2.0;

            FlightAltitude = BaseFlightAltitude + Math.Sin(_altitudePhase) * AltitudeBobAmplitude;
        }

        /// <summary>
        /// Если самолёт у края или за картой — разворачиваем курс внутрь карты.
        /// </summary>
        private Vector2Float ApplyMapBoundsSteering(Vector2Float desiredDir)
        {
            if (Game?.Map == null)
                return desiredDir;

            double min = 0.5;
            double maxX = Game.Map.Width - 0.5;
            double maxY = Game.Map.Length - 0.5;
            const double margin = 4.0;

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

            // За картой — только возврат. У края — подмешиваем к текущему курсу.
            if (outside)
                return inward;

            return (desiredDir + inward * 2.0).Normalize();
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

        private Vector2Float UpdateCombatFlight(double deltaTime)
        {
            Vector2Float targetPos = _attackTarget!.AimPosition;
            Vector2Float toTarget = targetPos - Position;
            double dist = toTarget.Magnitude();
            Vector2Float forward = GetForward2D();

            switch (_flightMode)
            {
                case FlightMode.Approach:
                {
                    if (dist < 0.001)
                        return forward;

                    // Нет боезапаса — не идём в пролёт, ждём перезарядку на уходе.
                    if (!CanFireRocket())
                    {
                        BeginBreakAway(forward);
                        return _breakAwayDir;
                    }

                    Vector2Float dirToTarget = toTarget.Normalize();
                    double angle = Vector2Float.AngleByVecotrs(forward, dirToTarget);

                    // Зашли на курс и дистанцию — начинаем огневой пролёт.
                    if (dist <= StrafeEnterDistance && !double.IsNaN(angle) && angle <= StrafeMaxAngle)
                    {
                        _flightMode = FlightMode.Strafe;
                        _shotsThisPass = 0;
                    }

                    return dirToTarget;
                }

                case FlightMode.Strafe:
                {
                    if (dist < 0.001)
                        return forward;

                    Vector2Float dirToTarget = toTarget.Normalize();
                    double along = Vector2Float.Dot(forward, dirToTarget);

                    // Пролетели цель (нос уже смотрит мимо / цель сзади) или отстреляли очередь.
                    bool passedTarget = along < 0.05 && dist <= PassCompleteDistance * 2.5;
                    bool closeOverfly = dist <= PassCompleteDistance;
                    bool emptiedMagazine = !CanFireRocket() || _shotsThisPass >= ShotsPerPass;
                    if (passedTarget || closeOverfly || emptiedMagazine)
                    {
                        BeginBreakAway(forward);
                        return _breakAwayDir;
                    }

                    // Во время пролёта держим курс через цель.
                    return dirToTarget;
                }

                case FlightMode.BreakAway:
                {
                    _breakAwayTraveled += CurrentSpeed * deltaTime;

                    // Сначала минимальный уход, потом ждём кулдаун магазина — и только тогда разворот на заход.
                    if (_breakAwayTraveled >= BreakAwayDistance && CanFireRocket())
                    {
                        _flightMode = FlightMode.Approach;
                        _shotsThisPass = 0;
                        return ComputeReapproachDir(targetPos, forward);
                    }

                    // Пока ждём перезарядку — широкий круг, без разворота на цель.
                    Vector2Float side = new Vector2Float(-_breakAwayDir.Y, _breakAwayDir.X);
                    double bank = _breakAwayTraveled >= BreakAwayDistance ? 0.55 : 0.25;
                    return (_breakAwayDir + side * bank).Normalize();
                }

                default:
                    _flightMode = FlightMode.Approach;
                    return (targetPos - Position).Normalize();
            }
        }

        private void BeginBreakAway(Vector2Float forward)
        {
            _flightMode = FlightMode.BreakAway;
            _breakAwayTraveled = 0;
            _breakAwayDir = forward.Magnitude() > 0.001 ? forward : GetForward2D();
        }

        private Vector2Float ComputeReapproachDir(Vector2Float targetPos, Vector2Float forward)
        {
            // Точка захода — за целью относительно текущего курса (заход с обратной стороны).
            Vector2Float inbound = forward.Magnitude() > 0.001 ? forward : (targetPos - Position).Normalize();
            Vector2Float reapproachPoint = targetPos - inbound * ReapproachDistance;
            Vector2Float toPoint = reapproachPoint - Position;
            if (toPoint.Magnitude() < 0.001)
                return inbound * -1.0;
            return toPoint.Normalize();
        }

        private Vector2Float UpdateCruiseFlight()
        {
            ClampCruisePointInsideMap();

            Vector2Float toPoint = _cruisePoint - Position;
            double dist = toPoint.Magnitude();

            // Далеко от точки — сначала долетаем, потом кружим.
            if (dist > OrbitRadius * 1.2)
                return toPoint.Normalize();

            return GetOrbitDirection(_cruisePoint);
        }

        /// <summary>Патруль по кругу вокруг точки удержания (не лететь прямо в край карты).</summary>
        private Vector2Float GetOrbitDirection(Vector2Float center)
        {
            Vector2Float offset = Position - center;
            double r = offset.Magnitude();

            Vector2Float radial;
            if (r < 0.25)
            {
                radial = GetForward2D();
                if (radial.Magnitude() < 0.001)
                    radial = new Vector2Float(1, 0);
                else
                    radial = radial.Normalize();
            }
            else
            {
                radial = offset * (1.0 / r);
            }

            // Точка впереди по окружности (против часовой).
            double cos = Math.Cos(OrbitLookAheadRadians);
            double sin = Math.Sin(OrbitLookAheadRadians);
            Vector2Float rotated = new(
                radial.X * cos - radial.Y * sin,
                radial.X * sin + radial.Y * cos);
            Vector2Float chase = center + rotated * OrbitRadius;

            Vector2Float toChase = chase - Position;
            if (toChase.Magnitude() < 0.001)
            {
                // Касательная, если уже почти на chase-точке.
                return new Vector2Float(-radial.Y, radial.X);
            }

            return toChase.Normalize();
        }

        private void EnsureCruisePoint()
        {
            if (_hasCruisePoint)
                return;

            _cruisePoint = Position;
            ClampCruisePointInsideMap();
            _hasCruisePoint = true;
        }

        private void ClampCruisePointInsideMap()
        {
            if (Game?.Map == null)
                return;

            double pad = OrbitRadius + 1.5;
            double min = pad;
            double maxX = Game.Map.Width - pad;
            double maxY = Game.Map.Length - pad;

            if (maxX <= min || maxY <= min)
            {
                min = 0.5;
                maxX = Math.Max(min, Game.Map.Width - 0.5);
                maxY = Math.Max(min, Game.Map.Length - 0.5);
            }

            _cruisePoint = new Vector2Float(
                Math.Clamp(_cruisePoint.X, min, maxX),
                Math.Clamp(_cruisePoint.Y, min, maxY));
        }

        private void RefreshAttackTarget()
        {
            IAttackTarget? fromPoints = null;
            foreach (BaseAttackingPoint point in AttackingPoints)
            {
                if (point.Target != null && !point.Target.IsDestroyed)
                {
                    fromPoints = point.Target;
                    break;
                }
            }

            if (fromPoints != null && !IsAttackMoving)
            {
                if (_attackTarget != fromPoints)
                {
                    _attackTarget = fromPoints;
                    _shotsThisPass = 0;
                    _flightMode = FlightMode.Approach;
                }
                return;
            }

            if (_attackTarget != null && _attackTarget.IsDestroyed)
                ClearAttackRun(clearWeaponTargets: true);

            // Attack-move: постоянно ищем ближайшую цель (даже если уже есть дальняя).
            if (CanAutoAcquireTargets || IsAttackMoving)
            {
                TryHuntNearestEnemy(AttackMoveHuntRange, preferCloserOnly: _attackTarget != null);
                return;
            }

            // Обычный приказ / выезд с завода — без автоохоты (иначе сразу летит на врага).
        }

        /// <summary>Самолёт «в покое» — кружит у точки приказа без боевого пролёта.</summary>
        protected override bool IsIdleForAggro()
        {
            if (_flightMode != FlightMode.Cruise)
                return false;
            if (!_hasCruisePoint)
                return false;
            return Vector2Float.Distance(Position, _cruisePoint) <= OrbitRadius * 1.35;
        }

        protected override void TryIdleAggro()
        {
            // Воздух охотится в RefreshAttackTarget (нужен Approach/strafe), не через наземный path.
        }

        protected override void TryRetargetNearestOnAttackMove()
        {
            // Воздух делает это в RefreshAttackTarget.
        }

        private void TryHuntNearestEnemy(double range, bool preferCloserOnly = false)
        {
            // Уже ведём цель — остаёмся в её домене (воздух↔земля не переключаем mid-fight).
            AttackTargetDomain? domainLock = null;
            if (_attackTarget != null && !_attackTarget.IsDestroyed)
                domainLock = _attackTarget.AttackDomain;

            IAttackTarget? best = FindNearestEngageableEnemyInDomain(range, domainLock);
            if (best == null)
                return;

            if (preferCloserOnly && _attackTarget != null && !_attackTarget.IsDestroyed)
            {
                if (ReferenceEquals(_attackTarget, best))
                    return;
                double dCur = Vector2Float.Distance(Position, _attackTarget.AimPosition);
                double dBest = Vector2Float.Distance(Position, best.AimPosition);
                if (dBest >= dCur - 0.75)
                    return;
            }

            foreach (BaseAttackingPoint point in AttackingPoints)
            {
                if (point.CanEngage(best))
                    point.SetTarget(best);
                else
                    point.ClearTarget();
            }

            _attackTarget = best;
            _shotsThisPass = 0;
            _breakAwayTraveled = 0;
            _flightMode = FlightMode.Approach;
        }

        /// <summary>Ближайший враг, который берёт хотя бы одна турель; опционально только свой домен.</summary>
        private IAttackTarget? FindNearestEngageableEnemyInDomain(double range, AttackTargetDomain? domainLock)
        {
            if (Game == null || range <= 0)
                return null;

            IAttackTarget? best = null;
            double bestDist = range;

            bool CanAnyTurretEngage(IAttackTarget t)
            {
                if (domainLock.HasValue && t.AttackDomain != domainLock.Value)
                    return false;
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

        private void FaceTowards(Vector2Float worldPoint)
        {
            Vector2Float dir = (worldPoint - Position);
            if (dir.Magnitude() < 0.01)
                return;
            // Rotation хранится так, что GetForward2D = VectorByAngle(-Rotation).
            double angle = Math.Atan2(dir.Y, dir.X) * (180.0 / Math.PI);
            Rotation = -angle;
        }

        private void ClearAttackRun(bool clearWeaponTargets = false)
        {
            if (clearWeaponTargets)
            {
                foreach (BaseAttackingPoint point in AttackingPoints)
                    point.ClearTarget();
            }

            _attackTarget = null;
            _shotsThisPass = 0;
            _breakAwayTraveled = 0;
            _flightMode = FlightMode.Cruise;
        }

        /// <summary>Вызывается пушкой при успешном выстреле в пролёте.</summary>
        public void NotifyShotFired()
        {
            if (_flightMode == FlightMode.Strafe)
                _shotsThisPass++;
        }

        private Vector2Float GetForward2D()
        {
            return Vector2Float.VectorByAngle(-Rotation).Normalize();
        }

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

            // На break-away крутимся чуть быстрее — чтобы быстрее развернуться на новый заход.
            double turnRate = _flightMode == FlightMode.BreakAway ? RotationSpeed * 1.25 : RotationSpeed;
            double rotationStep = turnRate * deltaTime;
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
