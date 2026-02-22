using RtsServer.App.Battle.Dto;

namespace RtsServer.App.Battle.Units
{
    public class Soldier : Unit
    {
        public Soldier(Vector2Int position, int playerOwner) : base("Soldier", 400, 400, position, playerOwner)
        {
            MaxSpeed = 2;
            RotationSpeed = 2;
        }
    }
}
