using RtsServer.App.Battle.Constructions;
using RtsServer.App.Battle.Dto;
using RtsServer.App.Battle.Interfaces;
using RtsServer.App.Battle.Tools;
using RtsServer.App.Battle.Units;

namespace RtsServer.App.Battle.Units.AttackingPoint
{
    public class BaseAttackingPoint(Unit currentUnit, float rotation = 0)
    {
        public Unit CurrentUnit { get; protected set; } = currentUnit;
        public double Rotation { get; protected set; } = rotation;
        public Vector2Float? TargetPoint { get; protected set; }
        public IAttackTarget? Target { get; protected set; }
        /// <summary>Растёт на каждый выстрел этой башни. Клиент по нему играет отдачу.</summary>
        public int ShotSerial { get; private set; }
        /// <summary>Куда целился последний выстрел (для клиентского трассера).</summary>
        public float LastShotAimX { get; private set; }
        public float LastShotAimY { get; private set; }
        public float Damage { get; protected set; }
        public double SpeedRotation { get; protected set; }
        public float Range { get; protected set; }
        /// <summary>Какие домены целей может атаковать эта турель.</summary>
        public AttackTargetDomain AllowedTargetDomains { get; protected set; } = AttackTargetDomain.Surface;
        /// <summary>Абсолютный урон по Light / Medium / Heavy.</summary>
        public ArmorDamageProfile ArmorDamage { get; protected set; } = ArmorDamageProfile.Zero;
        protected TypeTarget TypeTargetUnit { get; set; } = TypeTarget.Empty;

        protected const int KRotationSpeed = 2;

        protected double fireCooldown = 1.0;
        protected double _lastShotTime = 0;

        public virtual void Init()
        {
            Damage = 0;
            SpeedRotation = 0;
            Range = 0;
            fireCooldown = 1.0;
            _lastShotTime = 0;
            // По умолчанию — земля и здания (не воздух).
            AllowedTargetDomains = AttackTargetDomain.Surface;
            ArmorDamage = ArmorDamageProfile.Zero;
        }

        public bool CanEngage(IAttackTarget? target)
        {
            if (target == null || target.IsDestroyed)
                return false;
            return (AllowedTargetDomains & target.AttackDomain) != 0;
        }

        public void Update()
        {
            if (
                Target != null &&
                !Vector2Float.ReachDistance(CurrentUnit.Position, Target.AimPosition, Range)
            )
            {
                Target.OnDestroyAction -= LoseTarget;
                Target = null;
                TypeTargetUnit = TypeTarget.Empty;
            }

            if (Target != null && (Target.IsDestroyed || !OwnerCanSee(Target.AimPosition)))
            {
                LoseTarget();
            }

            if (
                Target == null &&
                TargetPoint.HasValue &&
                TargetPoint.Value != Vector2Float.Zero &&
                TypeTargetUnit != TypeTarget.Empty
            )
            {
                TypeTargetUnit = TypeTarget.Empty;
            }

            if (Target == null && TypeTargetUnit == TypeTarget.Empty)
            {
                RotateToUnitDirection();
            }

            Attack();
        }

        protected virtual void Attack()
        {
            if (TypeTargetUnit == TypeTarget.Unit && Target != null)
            {
                if (RotationToTarget(Target.AimPosition))
                {
                    TryFireAt(Target.AimPosition);
                }
            }
        }

        protected bool TryFireAt(
            Vector2Float targetPosition,
            string missileCode = "tank_shell",
            float missileSpeed = 20f,
            float explosionRange = 2f,
            double? targetHeightOverride = null,
            bool guided = false,
            bool flatTrajectory = false,
            bool preferLowArc = true,
            double loftDurationSeconds = 0,
            double? homingDurationSeconds = null,
            bool sharpDivertAfterLoft = false,
            bool ignoreCooldown = false,
            string? overrideMissileCode = null)
        {
            if (Target != null)
            {
                if (!OwnerCanSee(Target.AimPosition))
                    return false;
            }
            else if (!OwnerCanSee(targetPosition))
            {
                return false;
            }

            double now = CurrentUnit.Game.TimeSystem.GetTime();
            if (!ignoreCooldown && now - _lastShotTime < fireCooldown)
                return false;

            // Снаряды в 2 раза медленнее + дольше loft/homing (гравитация тоже /2 в Ballistics).
            missileSpeed = Ballistics.ScaleSpeed(missileSpeed);
            if (loftDurationSeconds > 0)
                loftDurationSeconds = Ballistics.ScaleDuration(loftDurationSeconds);
            if (homingDurationSeconds is > 0)
                homingDurationSeconds = Ballistics.ScaleDuration(homingDurationSeconds.Value);

            double fromHeight = CurrentUnit.GetMuzzleHeight();
            double toHeight = targetHeightOverride
                ?? (Target != null ? Target.GetBodyHeight() : 0.5);

            Vector3Float velocity;
            if (loftDurationSeconds > 0 || guided || flatTrajectory)
            {
                // Для loft передаём направление на цель — Missile сделает подъём с лёгким наклоном.
                Vector3Float toTarget = new(
                    targetPosition.X - CurrentUnit.Position.X,
                    targetPosition.Y - CurrentUnit.Position.Y,
                    toHeight - fromHeight);
                double mag = toTarget.Magnitude();
                if (mag < 1e-6)
                {
                    Vector2Float fwd = Vector2Float.VectorByAngle(-CurrentUnit.Rotation).Normalize();
                    velocity = new Vector3Float(fwd.X * missileSpeed, fwd.Y * missileSpeed, flatTrajectory ? 0 : missileSpeed);
                }
                else
                {
                    Vector3Float dir = toTarget.Normalized();
                    velocity = dir * missileSpeed;
                }
            }
            else if (!Ballistics.TryComputeLaunchVelocity(
                    CurrentUnit.Position,
                    fromHeight,
                    targetPosition,
                    toHeight,
                    missileSpeed,
                    out velocity,
                    preferLowArc))
            {
                return false;
            }

            Missile missile = new(
                overrideMissileCode ?? missileCode,
                CurrentUnit.Position,
                fromHeight,
                velocity,
                targetPosition,
                toHeight,
                missileSpeed,
                explosionRange,
                Damage,
                CurrentUnit.Game.TimeSystem,
                this,
                CurrentUnit.OwnerId,
                CurrentUnit.Game,
                lockTarget: guided ? Target : null,
                guided: guided,
                shooterUnitId: CurrentUnit.Id,
                applyGravity: !flatTrajectory && !guided && !sharpDivertAfterLoft,
                armorDamage: ArmorDamage,
                homingDurationSeconds: homingDurationSeconds,
                loftDurationSeconds: loftDurationSeconds,
                sharpDivertAfterLoft: sharpDivertAfterLoft);

            CurrentUnit.Game.AddMissile(missile);
            RegisterShot(targetPosition);
            return true;
        }

        /// <summary>
        /// Мгновенный импакт без снаряда: урон сразу, клиент рисует трассер по ShotSerial + LastShotAim.
        /// </summary>
        protected bool TryHitscanAt(IAttackTarget? target = null, bool ignoreCooldown = false)
        {
            target ??= Target;
            if (target == null || target.IsDestroyed || !CanEngage(target))
                return false;
            if (!OwnerCanSee(target.AimPosition))
                return false;

            double now = CurrentUnit.Game.TimeSystem.GetTime();
            if (!ignoreCooldown && now - _lastShotTime < fireCooldown)
                return false;

            float dmg = ArmorDamage.For(target.Armor);
            if (dmg > 0.01f)
                target.Health.TakeDamage(dmg);

            RegisterShot(target.AimPosition);
            return true;
        }

        private void RegisterShot(Vector2Float aim)
        {
            LastShotAimX = (float)aim.X;
            LastShotAimY = (float)aim.Y;
            ShotSerial++;
            _lastShotTime = CurrentUnit.Game.TimeSystem.GetTime();
        }

        public virtual void CheckTarget()
        {
            if (Target != null)
            {
                // Цель стала недопустимой или ушла из зрения — сбросить.
                if (!CanEngage(Target) || !OwnerCanSee(Target.AimPosition))
                    ClearTarget();
                else
                    return;
            }
            if (!CurrentUnit.CanAutoAcquireTargets) return;

            IAttackTarget? best = null;
            double bestDist = Range;

            foreach (Unit unit in CurrentUnit.Game.Units)
            {
                if (unit == CurrentUnit) continue;
                if (unit.OwnerId == CurrentUnit.OwnerId) continue;
                if (!CanEngage(unit) || !OwnerCanSee(unit.AimPosition)) continue;
                double dist = Vector2Float.Distance(CurrentUnit.Position, unit.AimPosition);
                if (dist >= bestDist) continue;
                bestDist = dist;
                best = unit;
            }

            foreach (Construction construction in CurrentUnit.Game.Constructions)
            {
                if (construction.OwnerId == CurrentUnit.OwnerId) continue;
                if (construction.IsDestroyed) continue;
                if (!CanEngage(construction) || !OwnerCanSee(construction.AimPosition)) continue;
                double dist = Vector2Float.Distance(CurrentUnit.Position, construction.AimPosition);
                if (dist >= bestDist) continue;
                bestDist = dist;
                best = construction;
            }

            if (best != null)
                SetTarget(best);
        }

        public virtual void SetTarget(Vector2Float targetPoint)
        {
            TargetPoint = targetPoint;
            Target = null;
            TypeTargetUnit = TypeTarget.Position;
        }

        public virtual void SetTarget(IAttackTarget target)
        {
            if (!CanEngage(target) || !OwnerCanSee(target.AimPosition))
                return;

            TargetPoint = null;
            Target = target;
            target.OnDestroyAction += LoseTarget;
            TypeTargetUnit = TypeTarget.Unit;
        }

        public void ClearTarget()
        {
            if (Target != null)
                Target.OnDestroyAction -= LoseTarget;
            LoseTarget();
        }

        protected virtual void LoseTarget()
        {
            Target = null;
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

        private bool OwnerCanSee(Vector2Float position)
        {
            return CurrentUnit.Game != null && CurrentUnit.Game.CanSee(CurrentUnit.OwnerId, position);
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
