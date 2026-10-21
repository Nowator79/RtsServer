using RtsServer.App.Battle.Dto;
using RtsServer.App.Battle.Interfaces;
using RtsServer.App.Battle.Units.AttackingPoint;

namespace RtsServer.App.Battle.Units
{
    /// <summary>Мобильная артиллерия на шасси танка ПВО: AGM по земле и зданиям.</summary>
    public class TankArtillery : Unit
    {
        public override ArmorType Armor => ArmorType.Heavy;

        public TankArtillery(Vector2Float position, int playerOwner)
            : base("TankArtillery", 1500, 1500, position, playerOwner)
        {
            MaxSpeed = 0.85;
            RotationSpeed = 85;
            AccelerationForce = 17;
            AttackingPoints = [new TankArtilleryCannon(this)];
        }
    }
}
