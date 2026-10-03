using RtsServer.App.Battle.Dto;
using RtsServer.App.Battle.Interfaces;
using RtsServer.App.Battle.Units.AttackingPoint;

namespace RtsServer.App.Battle.Units
{
    /// <summary>Пехота с гранатомётом — та же «модель» по масштабу, баллистический снаряд.</summary>
    public class Grenadier : Unit
    {
        public override ArmorType Armor => ArmorType.Light;

        public Grenadier(Vector2Float position, int playerOwner)
            : base("Grenadier", 450, 450, position, playerOwner)
        {
            MaxSpeed = 1.25;
            RotationSpeed = 150;
            AccelerationForce = 32;
            AttackingPoints = [new GrenadierLauncher(this)];
        }
    }
}
