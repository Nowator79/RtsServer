using RtsServer.App.Adapters;
using RtsServer.App.FileSystem;

namespace RtsServer.App.Buttle.MapButtle
{
    public class MapSceneFactory
    {

        public MapScene[] GetAllMapScene()
        {
            HashSet<MapScene> result = new();

            MapSceneFileManagereManager mapSceneFileManagereManager = new();

            result.Add(MapSceneAdapter.Get(mapSceneFileManagereManager.LoadMapByName("test")));

            return result.ToArray();
        }
    }
}
