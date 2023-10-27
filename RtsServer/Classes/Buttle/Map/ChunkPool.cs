using RtsServer.Classes.Buttle.Map.ChunksType;

namespace RtsServer.Classes.Buttle.Map
{
    public class ChunkPool
    {
        public Dictionary<string, ChunkBase> chunks = new();

        public ChunkPool()
        {
        }

        public void AddChunk(ChunkBase chunk)
        {
            Console.WriteLine(chunk.Name);
            chunks.Add(chunk.Name, chunk);
        }
    }
}
