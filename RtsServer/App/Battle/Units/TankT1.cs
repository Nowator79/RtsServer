using RtsServer.App.Battle.Dto;
using RtsServer.App.Battle.Units.AttackingPoint;

namespace RtsServer.App.Battle.Units
{
    public class TankT1 : Unit
    {
        public TankT1(Vector2Float position, int playerOwner) : base("TankT1", new(2000), position, playerOwner)
        {
            InitParameters();

        }
        public TankT1(Vector2Int position, int playerOwner) : base("TankT1", new(2000), position.GetFloat(), playerOwner)
        {
            InitParameters();
        }
        private void InitParameters()
        {
            MaxSpeed = 1;
            RotationSpeed = 100;
            AccelerationForce = 20;
            AttackingPoints = [new TankTower(this)];
        }
    }
}
