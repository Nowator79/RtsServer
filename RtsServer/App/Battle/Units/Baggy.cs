using RtsServer.App.Battle.Dto;
using RtsServer.App.Battle.Interfaces;
using RtsServer.App.Battle.Units.AttackingPoint;

namespace RtsServer.App.Battle.Units
{
    public class Baggy : Unit
    {
        public override ArmorType Armor => ArmorType.Medium;

        public Baggy(Vector2Float position, int playerOwner) : base("Baggy", 900, 900, position, playerOwner)
        {
            MaxSpeed = 1.8;
            RotationSpeed = 140;
            AccelerationForce = 28;
            AttackingPoints = [new BaggyMachineGun(this)];
        }

        public override double GetBodyHeight() => 0.35;
        public override double GetMuzzleHeight() => 0.4;
    }
}
