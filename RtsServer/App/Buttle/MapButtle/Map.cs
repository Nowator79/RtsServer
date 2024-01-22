using RtsServer.App.Buttle.MapButlle.ChunksType;
using RtsServer.App.FileSystem.Dto;

namespace RtsServer.App.Buttle.MapButlle
{
    public class Map
    {
        public List<ChunkBase> Chunks { get; set; }
        public ChunkBase[,] ChunksProcessed { get; set; }
        public int Width { get; private set; }
        public int Length { get; private set; }

        public Map(int width, int height)
        {
            Width = width;
            Length = height;

            Chunks = new();
        }

        public ChunkBase[,] GetArrayMap()
        {
            if (ChunksProcessed != null) return ChunksProcessed;

            ChunkBase[,] mapTmp = new ChunkBase[Width, Length];
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Length; y++)
                {
                    mapTmp[x, y] = Chunks[x * Length + y];
                }
            }
            ChunksProcessed = mapTmp;
            return mapTmp;
        }

    }
}
