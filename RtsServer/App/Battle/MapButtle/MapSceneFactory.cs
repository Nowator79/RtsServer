using RtsServer.App.Adapters;
using RtsServer.App.FileSystem;

namespace RtsServer.App.Battle.MapBattle
{
    public class MapSceneFactory
    {

        public MapScene[] GetAllMapScene()
        {
            var fileManager = new MapSceneFileManager();
            var result = new List<MapScene>();
            foreach (string name in fileManager.GetAvailableMapNames())
            {
                try
                {
                    result.Add(MapSceneAdapter.Get(fileManager.LoadMapByName(name)));
                }
                catch
                {
                    // пропускаем битые/нечитаемые карты
                }
            }
            return result.ToArray();
        }
    }
}
