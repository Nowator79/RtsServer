using RtsServer.App.Battle.Dto;

namespace RtsServer.App.Battle.Constructions
{
    public class Construction
    {
        public Health Health { get; protected set; }
        public Vector2Int Position { get; protected set; }
        public Vector2Int Size { get; protected set; }
        public string Code { get; protected set; }
        public int Id { get; set; }
        public Game Game { get; private set; }



        public Construction(
            Health health,
            Vector2Int position, 
            Vector2Int size, 
            string code
            )
        {
            Health = health;
            Position = position;
            Size = size;
            Code = code;
        }

        public void SetId(int Id)
        {
            this.Id = Id;
        }
        public void SetGame(Game game)
        {
            Game = game;
        }
    }
}
