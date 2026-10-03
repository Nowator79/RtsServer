using RtsServer.App.Battle.Constructions;
using RtsServer.App.Battle.Dto;
using RtsServer.App.NetWorkDto;

namespace RtsServer.App.Adapters
{
    public class ConstructionAdapter
    {
        public static NConstruction Get(Construction construction)
        {
            float productionProgress = 0f;
            string productionUnitCode = "";
            string productionQueue = "";

            if (construction is MilitaryFactoryConstruction military)
            {
                productionProgress = military.ProductionProgress;
                productionUnitCode = military.CurrentProductionCode ?? "";
                if (military.ProductionQueue.Count > 0)
                    productionQueue = string.Join(",", military.ProductionQueue);
            }
            else if (construction is AirFactoryConstruction air)
            {
                productionProgress = air.ProductionProgress;
                productionUnitCode = air.CurrentProductionCode ?? "";
                if (air.ProductionQueue.Count > 0)
                    productionQueue = string.Join(",", air.ProductionQueue);
            }
            else if (construction is BarracksConstruction barracks)
            {
                productionProgress = barracks.ProductionProgress;
                productionUnitCode = barracks.CurrentProductionCode ?? "";
                if (barracks.ProductionQueue.Count > 0)
                    productionQueue = string.Join(",", barracks.ProductionQueue);
            }

            float buildDuration = construction.IsBuilt ? 0f : construction.BuildDurationSeconds;

            return new NConstruction(
                construction.Id,
                construction.Code,
                construction.Health.Value,
                (Vector2Int)construction.Position,
                construction.BuildProgress,
                construction.OwnerId,
                construction.Health.Max,
                construction.IsPowered,
                productionProgress,
                productionUnitCode,
                productionQueue,
                buildDuration);
        }
    }
}
