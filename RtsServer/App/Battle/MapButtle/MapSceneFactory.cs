using RtsServer.App.Adapters;
using RtsServer.App.FileSystem;

namespace RtsServer.App.Battle.MapBattle
{
    public class MapSceneFactory
    {

        public MapScene[] GetAllMapScene()
        {
            HashSet<MapScene> result = new();

            MapSceneFileManager mapSceneFileManagereManager = new();

            result.Add(MapSceneAdapter.Get(mapSceneFileManagereManager.LoadMapByName("test")));

            return result.ToArray();
        }
    }
}
