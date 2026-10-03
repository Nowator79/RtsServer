using RtsServer.App.Battle.Constructions;
using RtsServer.App.Battle.Dto;
using RtsServer.App.Battle.MapBattle;
using RtsServer.App.Battle.Units;
using RtsServer.App.FileSystem;
using RtsServer.App.FileSystem.Dto;

namespace RtsServer.App.Adapters
{
    public class MapSceneAdapter
    {
        public static MapScene Get(FMapScene mapScene)
        {
            HashSet<Construction> constructions = new();
            if (mapScene.Constructions != null)
            {
                foreach (FConstruction construction in mapScene.Constructions)
                {
                    constructions.Add(ConstructionFactory.GetByCode(construction.Code, construction.Position, construction.PlayerOwnerNum));
                }
            }

            HashSet<Unit> units = new();
            if (mapScene.Units != null)
            {
                foreach (FUnit unit in mapScene.Units)
                {
                    units.Add(UnitFactory.GetByCode(unit.Code, unit.Position, unit.PlayerOwnerNum));
                }
            }

            MapFileManager mapFileManager = new();
            MapScene scene = new(
                MapAdapter.Get(mapFileManager.LoadMapByCode(mapScene.MapCode)),
                constructions.ToArray(),
                units.ToArray()
            )
            {
                StartingResources = mapScene.StartingResources > 0
                    ? mapScene.StartingResources
                    : MapScene.DefaultStartingResources
            };

            if (mapScene.PlayerStarts != null)
            {
                foreach (FPlayerStart playerStart in mapScene.PlayerStarts)
                {
                    scene.CameraStartsByPlayer[playerStart.PlayerOwnerNum] = playerStart.CameraPosition;
                }
            }

            return scene;
        }
    }
}
