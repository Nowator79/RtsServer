using RtsServer.App.Buttle.Dto;

namespace RtsServer.App.Buttle.Units
{
    public class TankT1 : Unit
    {
        public TankT1(Vector2Float position) : base("TankT1", new(2000), position)
        {
            Speed = 1.2f;
        }
    }
}
