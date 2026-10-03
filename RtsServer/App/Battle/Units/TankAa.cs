using RtsServer.App.Battle.Dto;
using RtsServer.App.Battle.Interfaces;
using RtsServer.App.Battle.Units.AttackingPoint;

namespace RtsServer.App.Battle.Units
{
    /// <summary>Мобильная ПВО: бьёт только воздух ракетами AAM.</summary>
    public class TankAa : Unit
    {
        public override ArmorType Armor => ArmorType.Heavy;

        public TankAa(Vector2Float position, int playerOwner)
            : base("TankAa", 1600, 1600, position, playerOwner)
        {
            MaxSpeed = 0.9;
            RotationSpeed = 90;
            AccelerationForce = 18;
            AttackingPoints = [new TankAaCannon(this)];
        }
    }
}
