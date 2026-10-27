using RtsServer.App.Battle.Constructions;
using RtsServer.App.Battle.Dto;
using RtsServer.App.Battle.Interfaces;
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

            if (construction is IUnitProductionHost host)
            {
                productionProgress = host.ProductionProgress;
                productionUnitCode = host.CurrentProductionCode ?? "";
                productionQueue = host.ProductionQueueCsv;
            }

            float buildDuration = (construction.IsBuilt && !construction.IsPacking)
                ? 0f
                : construction.BuildDurationSeconds;

            int shotSerial = 0;
            float shotAimX = 0f;
            float shotAimY = 0f;
            float turretRotation = 0f;
            if (construction is IWeaponFxSource fx)
            {
                shotSerial = fx.ShotSerial;
                shotAimX = fx.ShotAimX;
                shotAimY = fx.ShotAimY;
                turretRotation = fx.TurretRotation;
            }

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
                buildDuration,
                shotSerial,
                shotAimX,
                shotAimY,
                turretRotation,
                construction.FacingYaw,
                construction.IsPacking);
        }
    }
}
