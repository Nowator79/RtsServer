using RtsServer.App.Battle.Dto;

namespace RtsServer.App.Battle.Units
{
    public class Soldier : Unit
    {
        public Soldier(Vector2Float position, int playerOwner) : base("Soldier", new(100), position, playerOwner)
        {
            Speed = 2;
            RotationSpeed = 2;
        }
    }
}
