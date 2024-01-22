using RtsServer.App.Buttle.MapButlle;
using RtsServer.App.FileSystem.Dto;

namespace RtsServer.App.Adapters
{
    public class MapAdapter
    {
        public static FMap Get(Map map)
        {
            FMap resMap = new FMap();
            resMap.Width = map.Width;
            resMap.Length = map.Length;
            resMap.Chunks = new();
            map.Chunks.ForEach(chunk =>
            {
                resMap.Chunks.Add(new FChank(chunk.Id, chunk.Height));
            });

            return resMap;
        }

        public static Map Get(FMap map)
        {
            Map resMap = new(map.Width, map.Length);

            map.Chunks.ForEach(chunk =>
            {
                resMap.Chunks.Add(new Buttle.MapButlle.ChunksType.ChunkBase(chunk.TypeId, chunk.Height));
            });

            return resMap;
        }
    }
}
