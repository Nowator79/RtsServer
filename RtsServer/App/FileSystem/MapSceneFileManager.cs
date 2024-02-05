using RtsServer.App.FileSystem.Dto;
using System.Text.Json;

namespace RtsServer.App.FileSystem
{
    public class MapSceneFileManagereManager : FileManager
    {
        public MapSceneFileManagereManager()
        {
            path = @"D:\Radium Rts\MapScene";
            format = "json";
        }

        public FMapScene LoadMapByName(string name)
        {
            string fileText = File.ReadAllText(@$"{path}\{name}.json");
            return JsonSerializer.Deserialize<FMapScene>(fileText);
        }

        public void SaveMapByName(FMapScene map, string name)
        {
            string text = JsonSerializer.Serialize(map);

            string pathFile = path + name + "." + format;

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            using (StreamWriter writer = new(pathFile, false))
            {
                writer.WriteLineAsync(text).Wait();
            }
        }
    }
}
