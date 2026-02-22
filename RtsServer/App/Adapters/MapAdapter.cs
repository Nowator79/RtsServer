using RtsServer.App.Battle.MapBattle;
using RtsServer.App.FileSystem.Dto;
using RtsServer.App.NetWorkDto;

namespace RtsServer.App.Adapters
{
    public class MapAdapter
    {
        public static FMap Get(Map map)
        {
            FMap resMap = new();
            resMap.Width = map.Width;
            resMap.Length = map.Length;
            resMap.Chunks = new();
            map.Chunks.ForEach(chunk =>
            {
                resMap.Chunks.Add(new FTile(chunk.Id, chunk.Height));
            });

            return resMap;
        }

        public static Map Get(FMap map)
        {
            Map resMap = new(map.Width, map.Length);
            resMap.SetCode(map.Name);
            map.Chunks.ForEach(chunk =>
            {
                resMap.Chunks.Add(new Battle.MapBattle.ChunksType.ChunkBase(chunk.TypeId, chunk.Height));
            });

            return resMap;
        }

        public static NMap GetNMap(FMap map, string code)
        {
            NMap NMap = new(map.Width, map.Length)
            {
                Name = map.Name,
                Code = code
            };
            NMap.Tiles = [];
            map.Chunks.ForEach(tile =>
            {
                NMap.Tiles.Add(new NTile(tile.TypeId, tile.Height));
            });

            return NMap;
        }
    }
}
