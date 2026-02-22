using RtsServer.App.Battle.Dto;

namespace RtsServer.App.Battle.Constructions
{
    public abstract class Construction : BattleEntity
    {
        public Health Health { get; protected set; }
        public Vector2Int Size { get; protected set; }

        public Construction(
            int health,
            int maxhealth,
            Vector2Int position,
            Vector2Int size,
            string code,
            int playerOwner
            ) : base(code, playerOwner, position)
        {
            Health = new(health, maxhealth, this);
            Size = size;
        }

        public void SetId(int Id)
        {
            this.Id = Id;
        }
       
    }
}
