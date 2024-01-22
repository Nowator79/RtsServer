using RtsServer.App.Buttle.MapButlle.ChunksType;

namespace RtsServer.App.Buttle.MapButlle.MapGenerator
{
    public class GeneratorBase
    {
        public GeneratorBase()
        {
        }

        public void Start(Map map)
        {
            for (int x = 0; x < map.Width; x++)
            {
                for (int y = 0; y < map.Width; y++)
                {
                    map.Chunks.Add(new ChunkBase(1));
                }
            }
        }
    }
}
