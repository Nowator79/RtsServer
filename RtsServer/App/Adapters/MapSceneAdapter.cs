using RtsServer.App.Buttle.Constructions;
using RtsServer.App.Buttle.MapButtle;
using RtsServer.App.Buttle.Units;
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
                units.Add(UnitFactory.GetByCode(unit.Code, unit.Position));
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
