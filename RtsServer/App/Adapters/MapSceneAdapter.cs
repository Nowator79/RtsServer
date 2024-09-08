using RtsServer.App.Battle.Constructions;
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
            foreach (FConstruction constuctin in mapScene.Constuctins)
            {
                constructions.Add(ConstructionFactory.GetByCode(constuctin.Code, constuctin.Position));
            }

            HashSet<Unit> units = new();
            foreach (FUnit unit in mapScene.Units)
            {
                units.Add(UnitFactory.GetByCode(unit.Code, unit.Position, unit.PlayerOwnerNum));
            }

            MapFileManager mapFileManager = new();
            return new MapScene(
                MapAdapter.Get(mapFileManager.LoadMapByName(mapScene.MapCode)),
                constructions.ToArray(),
                units.ToArray()
                );
        }
    }
}
