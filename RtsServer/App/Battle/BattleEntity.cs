using RtsServer.App.Battle.Dto;
using RtsServer.App.Battle.Units.AttackingPoint;

namespace RtsServer.App.Battle
{
    public abstract class BattleEntity : IDisposable
    {

        public int Id { get; protected set; }
        public string Code { get; protected set; }
        public bool IsDestroyed { get; protected set; }
        public Action? OnDestroyAction { get; set; }
        public int OwnerId { get; protected set; }
        public Vector2Float Position { get; protected set; }
        public Game Game { get; private set; }

        public BattleEntity(string code, int playerOwner, Vector2Float position)
        {
            Code = code ?? throw new ArgumentNullException(nameof(code));
            OwnerId = playerOwner;
            Position = position;
        }

        public virtual void Init(Game game)
        {
            Game = game;
        }

        public virtual void Destroy()
        {
            IsDestroyed = true;
            OnDestroyAction?.Invoke();
        }

        public virtual void Update()
        {

        }

        public virtual void Dispose()
        {
        }

        public void SetGame(Game game)
        {
            Game = game;
        }
    }
}
