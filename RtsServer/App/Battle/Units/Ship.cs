using RtsServer.App.Battle.Dto;
using RtsServer.App.Battle.Interfaces;
using RtsServer.App.Battle.Units.AttackingPoint;

namespace RtsServer.App.Battle.Units
{
    /// <summary>Корабль: ходит только по воде, оружие как у танка + САУ.</summary>
    public class Ship : Unit
    {
        public override bool MovesOnWater => true;
        public override ArmorType Armor => ArmorType.Heavy;

        public Ship(Vector2Float position, int playerOwner)
            : base("Ship", 2800, 2800, position, playerOwner)
        {
            MaxSpeed = 1.15;
            RotationSpeed = 70;
            AccelerationForce = 14;
            AttackingPoints = [new TankTower(this), new TankArtilleryCannon(this)];
        }

        public override double GetBodyHeight() => GetGroundElevation() + 0.7;
        public override double GetMuzzleHeight() => GetGroundElevation() + 0.9;
    }
}
