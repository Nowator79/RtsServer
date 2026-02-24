using RtsServer.App.FileSystem.Dto;
using System.Text.Json;

namespace RtsServer.App.FileSystem
{
    public class MapSceneFileManager : FileManager
    {
        public MapSceneFileManager()
        {
            path = Path.Combine(MainFolder, "MapScene");
            format = "json";
        }

        public FMapScene LoadMapByName(string name)
        {
            string filePath = Path.Combine(path, $"{name}.{format}");

            // Если файла нет (чистый проект/новая машина) — создаём дефолтную карту.
            if (!File.Exists(filePath))
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                var defaultMap = CreateDefaultMapScene(name);
                SaveMapByName(defaultMap, name);
                return defaultMap;
            }

            string fileText = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<FMapScene>(fileText);
        }

        public void SaveMapByName(FMapScene map, string name)
        {
            string text = JsonSerializer.Serialize(map);

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            string pathFile = Path.Combine(path, $"{name}.{format}");

            using (StreamWriter writer = new(pathFile, false))
            {
                writer.WriteLineAsync(text).Wait();
            }
        }

        private static FMapScene CreateDefaultMapScene(string name)
        {
            return new FMapScene
            {
                MapCode = name,
                Constructions = Array.Empty<FConstruction>(),
                Units = Array.Empty<FUnit>()
            };
        }
    }
}
