using RtsServer.Classes.Buttle.Map.ChunksType;

namespace RtsServer.Classes.Buttle.Map.MapGenerator
{
    public class GeneratorBase
    {
        private readonly ChunkPool chunkPool;
        public GeneratorBase(ChunkPool chunkPool)
        {
            this.chunkPool = chunkPool;
        }

        public void Start(MapBase map)
        {
            for (int x = 0; x < map.Size; x++)
            {
                for (int y = 0; y < map.Size; y++)
                {
                    map.Chunks.Add(chunkPool.chunks[ChunkBase.NameChunk]);
                }
            }
        }
    }
}
