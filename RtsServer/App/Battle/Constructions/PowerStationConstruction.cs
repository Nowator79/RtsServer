using RtsServer.App.Battle.Dto;
using RtsServer.App.Battle.Interfaces;

namespace RtsServer.App.Battle.Constructions
{
    /// <summary>Электростанция — даёт энергию, без склада ресурсов.</summary>
    public class PowerStationConstruction : Construction, IEnergyProvider
    {
        public const int sizeX = 2;
        public const int sizeY = 2;
        public const int maxHealth = 3000;
        public const string Code = "PowerStation";
        public const int BuildCost = 100;
        public const float BuildTime = 6f;

        public int EnergyProvided => IsBuilt ? 100 : 0;

        public PowerStationConstruction(Vector2Int position, int playerOwner)
            : base(maxHealth, maxHealth, position, new(sizeX, sizeY), Code, playerOwner) { }

        public override void Update()
        {
            base.Update();
        }
    }
}
