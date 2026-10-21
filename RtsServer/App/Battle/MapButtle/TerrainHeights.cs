using RtsServer.App.Battle.Dto;

namespace RtsServer.App.Battle.MapBattle
{
    /// <summary>
    /// Высота рельефа на сервере. Scale должен совпадать с client MapTerrainHeights.HeightScale.
    /// </summary>
    public static class TerrainHeights
    {
        public const double HeightScale = 0.3125;

        public static double SampleAt(Game? game, Vector2Float position)
        {
            if (game?.Map == null)
                return 0;

            Map map = game.Map;
            int x = (int)position.X;
            int y = (int)position.Y;
            if (x < 0 || y < 0 || x >= map.Width || y >= map.Length)
                return 0;

            try
            {
                ChunksType.ChunkBase chunk = map.GetChunk(x, y);
                if (chunk == null || chunk.Height <= 0)
                    return 0;
                return chunk.Height * HeightScale;
            }
            catch
            {
                return 0;
            }
        }

        public static double SampleAt(Game? game, Vector2Int cell)
        {
            return SampleAt(game, (Vector2Float)cell);
        }
    }
}
