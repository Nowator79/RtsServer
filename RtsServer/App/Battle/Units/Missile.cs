using RtsServer.App.Battle.Dto;
using RtsServer.App.Battle.Explosion;
using RtsServer.App.Battle.Units.AttackingPoint;
using RtsServer.App.Tools;

namespace RtsServer.App.Battle.Units
{
    public class Missile : BattleEntity
    {
        public int Id { get; private set; }
        public string Code { get; }
        public Vector2Float Target { get; private set; }
        public float Speed { get; private set; }
        public BaseAttackingPoint CurrentAttackPoint { get; private set; }
        public float Range { get; private set; }
        public float Damage { get; private set; }

        public Game Game => throw new NotImplementedException();

        private readonly TimeSystem _timeSystem;
        public void SetId(int id) => Id = id;

        public Missile(string code, Vector2Float startPosition, Vector2Float target, float speed, float range, float damage, TimeSystem timeSystem, BaseAttackingPoint CurrentAttackPoint, int playerOwner) : base(code, playerOwner, startPosition)
        {
            Code = code;
            Position = startPosition;
            Target = target;
            Speed = speed;
            IsDestroyed = false;
            _timeSystem = timeSystem;
            this.CurrentAttackPoint = CurrentAttackPoint;
            Range = range;
            Damage = damage;
        }

        public void Update()
        {
            if (IsDestroyed)
                return;

            float deltaTime = (float)_timeSystem.GetDelta();

            if (Vector2Float.ReachDistance(Position, Target, Speed * deltaTime))
            {
                Position = Target;
                Destroy();

            }
            else
            {
                Vector2Float direction = (Target - Position).Normalize();
                Vector2Float movement = direction * Speed * deltaTime;
                Position += movement;
            }
        }

        public override void Destroy()
        {
            MapBattle.Map map = CurrentAttackPoint.CurrentUnit.Game.Map;
            BaseExplosion Explosion = new(Range, Damage, Position, map);
            Explosion.Explode();

            base.Destroy();
        }
    }
}
