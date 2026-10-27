using RtsServer.App.Battle.Dto;
using RtsServer.App.Battle.Interfaces;

namespace RtsServer.App.Battle.Constructions
{
    /// <summary>
    /// Стационарный кран: расширяет зону строительства (как развёрнутый МСЦ).
    /// </summary>
    public class CraneConstruction : Construction, IEnergyConsumer
    {
        public const int sizeX = 1;
        public const int sizeY = 1;
        public const int maxHealth = 2200;
        public const string Code = "Crane";
        public const int BuildCost = 180;
        public const float BuildTime = 8f;
        public const int BuildZoneRange = 8;

        public int EnergyRequired => IsBuilt ? 12 : 0;

        public override ArmorType Armor => ArmorType.Medium;

        public CraneConstruction(Vector2Int position, int playerOwner)
            : base(maxHealth, maxHealth, position, new(sizeX, sizeY), Code, playerOwner)
        {
        }

        public override double GetBodyHeight() => GetGroundElevation() + 2.8;
    }
}
