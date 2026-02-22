using RtsServer.App.Battle.Dto;
using RtsServer.App.Battle.MapBattle.ChunksType;

namespace RtsServer.App.Battle.MapBattle
{
    public class Map
    {
        public List<ChunkBase> Chunks { get; private set; }
        public ChunkBase[,] ChunksProcessed { get; private set; }

        public int Width { get; private set; }
        public int Length { get; private set; }
        public string Code { get; private set; }

        public Map(int width, int length)
        {
            Width = width;
            Length = length;
            Chunks = new List<ChunkBase>(width * length);
        }

        public void SetCode(string code)
        {
            Code = code;
        }

        public ChunkBase[,] GetArrayMap()
        {
            if (ChunksProcessed != null) return ChunksProcessed;

            if (Chunks == null || Chunks.Count != Width * Length)
                throw new InvalidOperationException("Chunks list is not properly initialized.");

            ChunkBase[,] mapTmp = new ChunkBase[Width, Length];

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Length; y++)
                {
                    mapTmp[x, y] = Chunks[x * Length + y];
                }
            }

            ChunksProcessed = mapTmp;
            return ChunksProcessed;
        }

        public ChunkBase GetChunk(int x, int y)
        {
            return GetChunkInternal(x, y);
        }

        public ChunkBase GetChunkAtPosition(Vector2Float position)
        {
            int x = (int)position.X;
            int y = (int)position.Y;
            return GetChunkInternal(x, y);
        }

        private ChunkBase GetChunkInternal(int x, int y)
        {
            if (ChunksProcessed == null)
                GetArrayMap();

            if (x < 0 || y < 0 || x >= Width || y >= Length)
                throw new ArgumentOutOfRangeException($"Chunk coordinates ({x}, {y}) out of bounds.");

            return ChunksProcessed[x, y];
        }

        public List<ChunkBase> GetChunksInRadius(Vector2Float center, float radius)
        {
            if (ChunksProcessed == null)
                GetArrayMap();

            List<ChunkBase> result = new();
            int minX = Math.Max(0, (int)(center.X - radius));
            int maxX = Math.Min(Width - 1, (int)(center.X + radius));
            int minY = Math.Max(0, (int)(center.Y - radius));
            int maxY = Math.Min(Length - 1, (int)(center.Y + radius));

            float radiusSq = radius * radius;

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    var chunkCenter = new Vector2Float(x + 0.5f, y + 0.5f);
                    if (Vector2Float.DistanceSQRT(center, chunkCenter) <= radiusSq)
                    {
                        result.Add(ChunksProcessed[x, y]);
                    }
                }
            }

            return result;
        }
    }
}
