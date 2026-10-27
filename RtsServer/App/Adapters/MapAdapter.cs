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
            resMap.Ore = map.Ore.ToFileList();
            resMap.Trees = map.Trees.ToFileList();

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

            if (map.Ore != null && map.Ore.Count > 0)
                resMap.Ore.LoadFrom(map.Ore);
            else
                resMap.Ore.GenerateDefaultPatches(resMap);

            if (map.Trees != null && map.Trees.Count > 0)
                resMap.Trees.LoadFrom(map.Trees);

            return resMap;
        }

        public static NMap GetNMap(FMap map, string code)
        {
            Map runtime = Get(map);
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
            NMap.Ore = [];
            foreach (var (x, y, amount) in runtime.Ore.Enumerate())
                NMap.Ore.Add(new NOreCell(x, y, amount));
            NMap.Trees = [];
            foreach (var (x, y, variant) in runtime.Trees.Enumerate())
                NMap.Trees.Add(new NTreeCell(x, y, variant));

            return NMap;
        }
    }
}
