using RtsServer.App.Battle.Dto;
using RtsServer.App.Battle.Interfaces;
using RtsServer.App.Battle.Units.AttackingPoint;

namespace RtsServer.App.Battle.Units
{
    public class Soldier : Unit
    {
        public override ArmorType Armor => ArmorType.Light;
        public override double CollisionRadius => 0.1;

        public Soldier(Vector2Float position, int playerOwner)
            : base("Soldier", 400, 400, position, playerOwner)
        {
            MaxSpeed = 1.4;
            RotationSpeed = 160;
            AccelerationForce = 35;
            AttackingPoints = [new SoldierRifle(this)];
        }
    }
}
