using RtsServer.App.FileSystem.Dto;
using System.Text.Json;

namespace RtsServer.App.FileSystem
{
    public class MapFileManager : FileManager
    {
        public MapFileManager()
        {
            path = MainFolder  + @"Map";
            format = "json";
        }

        public FMap LoadMapByName(string name)
        {
            string fileText = File.ReadAllText(@$"{path}\{name}.json");
            return JsonSerializer.Deserialize<FMap>(fileText);
        }

        public void SaveMapByName(FMap map, string name)
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
