using RtsServer.App.FileSystem.Dto;
using System.Text.Json;

namespace RtsServer.App.FileSystem
{
    public class MapFileManager : FileManager
    {
        public MapFileManager()
        {
            path = Path.Combine(MainFolder, "Map");
            format = "json";
        }

        public FMap LoadMapByCode(string code)
        {
            string filePath = Path.Combine(path, $"{code}.{format}");

            // Если файла карты нет — создаём простую дефолтную карту,
            // чтобы сервер мог стартовать на чистой машине.
            if (!File.Exists(filePath))
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                var defaultMap = CreateDefaultMap(code);
                SaveMapByCode(defaultMap, code);
                return defaultMap;
            }

            string fileText = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<FMap>(fileText);
        }

        public void SaveMapByCode(FMap map, string code)
        {
            string text = JsonSerializer.Serialize(map);

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            string pathFile = Path.Combine(path, $"{code}.{format}");

            using (StreamWriter writer = new(pathFile, false))
            {
                writer.WriteLineAsync(text).Wait();
            }
        }

        private static FMap CreateDefaultMap(string code)
        {
            return new FMap
            {
                Name = code,
                Width = 16,
                Length = 16,
                Chunks = new List<FTile>()
            };
        }
    }
}
