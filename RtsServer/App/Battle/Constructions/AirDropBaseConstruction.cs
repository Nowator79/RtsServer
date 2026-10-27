using RtsServer.App.Battle.Dto;
using RtsServer.App.Battle.Interfaces;
using RtsServer.App.Battle.Units;

namespace RtsServer.App.Battle.Constructions
{
    /// <summary>
    /// Развёрнутый воздушный транспортник (1×1). Зона строительства 7×7. Сворачивается в AirTransporter.
    /// </summary>
    public class AirDropBaseConstruction : Construction, IEnergyProvider, IEnergyConsumer, IUndeployableConstruction
    {
        public const int sizeX = 1;
        public const int sizeY = 1;
        public const int maxHealth = 2200;
        public const string Code = "AirDropBase";
        /// <summary>0 — только через deploy.</summary>
        public const int BuildCost = 0;
        public const float BuildTime = 0f;
        /// <summary>Зазор до зоны: 1×1 + 3 клетки = площадка 7×7.</summary>
        public const int BuildZoneRange = 3;

        public string PackedUnitCode => "AirTransporter";

        public int EnergyProvided => IsBuilt ? 8 : 0;
        public int EnergyRequired => IsBuilt ? 4 : 0;

        public override ArmorType Armor => ArmorType.Medium;

        public AirDropBaseConstruction(Vector2Int position, int playerOwner)
            : base(maxHealth, maxHealth, position, new(sizeX, sizeY), Code, playerOwner)
        {
        }

        public override double GetBodyHeight() => GetGroundElevation() + 1.0;

        public bool TryUndeploy()
        {
            if (Game == null || IsDestroyed || !IsBuilt)
                return false;

            // GetFloat = (X,Y) — совпадает с визуальным центром клетки (MapGrid −0.5).
            Vector2Int cell = Position.ToInt();
            Vector2Float spawnPos = cell.GetFloat();

            try
            {
                Unit unit = UnitFactory.GetByCode(PackedUnitCode, spawnPos, OwnerId);
                unit.SetGame(Game);
                Game.UnitsForAdd.Add(unit);

                Destroy(suppressDeathFx: true);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
