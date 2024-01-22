using RtsServer.App.Buttle.Dto;

namespace RtsServer.App.Buttle.Units
{
    public class Soldier : Unit
    {
        public Soldier(Vector2Float position) : base("Soldier", new(100), position)
        {
            Speed = 2;
            RotationSpeed = 2;
        }
    }
}
