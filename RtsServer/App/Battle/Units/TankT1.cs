using RtsServer.App.Battle.Dto;

namespace RtsServer.App.Battle.Units
{
    public class TankT1 : Unit
    {
        public TankT1(Vector2Float position, int playerOwner) : base("TankT1", new(2000), position, playerOwner)
        {
            Speed = 70;
            RotationSpeed = 100;
        }
        public TankT1(Vector2Int position, int playerOwner) : base("TankT1", new(2000), position, playerOwner)
        {
            Speed = 70;
            RotationSpeed = 100;
        }
    }
}
