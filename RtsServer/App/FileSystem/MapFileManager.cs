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

        public FMap LoadMapByCode(string code)
        {
            string fileText = File.ReadAllText(@$"{path}\{code}.json");
            return JsonSerializer.Deserialize<FMap>(fileText);
        }

        public void SaveMapByCode(FMap map, string code)
        {
            string text = JsonSerializer.Serialize(map);

            string pathFile = path + code + "." + format;

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
