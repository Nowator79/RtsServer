using RtsServer.Classes.Buttle.Map;
using RtsServer.Classes.Buttle.Map.MapGenerator;

namespace RtsServer.Classes.Buttle
{
    public class Game
    {
        public MapBase? Map { get; set; }

        public void GenerateRandomMap()
        {
            Map = new(10);
        }

        public void Start()
        {
            ChunkPool chunkPool = new();
            Map.ChunkPool = chunkPool;
            chunkPool.AddChunk(new Map.ChunksType.ChunkBase());
            chunkPool.AddChunk(new Map.ChunksType.ChunkWoter());

            GeneratorBase generator = new(chunkPool);
            generator.Start(Map);
        }

        public void Update()
        {

        }

        public void View()
        {
            for (int x = 0; x < Map.Size; x++)
            {
                for (int y = 0; y < Map.Size; y++)
                {
                    Console.Write($" {Map.Chunks[x * 10 + y].Name} ");
                }
                Console.WriteLine();
            }
        }
    }
}
