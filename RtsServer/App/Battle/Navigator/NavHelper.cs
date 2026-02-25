using RtsServer.App.Battle.Dto;
using System.Collections.Generic;

namespace RtsServer.App.Battle.Navigator
{
    public static class NavHelper
    {
        public static float DistanceSQRT(Vector2Int start, Vector2Int end)
        {
            int dx = end.X - start.X, dy = end.Y - start.Y;
            return (float)(dx * dx + dy * dy);
        }

        public static float Distance(Vector2Int start, Vector2Int end)
        {
            return (float)Math.Sqrt(DistanceSQRT(start, end));
        }

        public static Vector2Int[] GetNear(Vector2Int vector2Int)
        {
            Vector2Int[] result = new Vector2Int[8];
            result[0] = new(vector2Int.X + 1, vector2Int.Y + 0);
            result[1] = new(vector2Int.X + 1, vector2Int.Y + 1);
            result[2] = new(vector2Int.X + 0, vector2Int.Y + 1);
            result[3] = new(vector2Int.X - 1, vector2Int.Y + 1);
            result[4] = new(vector2Int.X - 1, vector2Int.Y + 0);
            result[5] = new(vector2Int.X - 1, vector2Int.Y - 1);
            result[6] = new(vector2Int.X + 0, vector2Int.Y - 1);
            result[7] = new(vector2Int.X + 1, vector2Int.Y - 1);
            return result;
        }

        public static Vector2Int[] GetSafeNear(Vector2Int vector2Int, int width, int length)
        {
            var near = GetNear(vector2Int);
            var list = new List<Vector2Int>(8);
            for (int i = 0; i < near.Length; i++)
            {
                var p = near[i];
                if (p.X >= 0 && p.Y >= 0 && p.X < width && p.Y < length)
                    list.Add(p);
            }
            return list.ToArray();
        }

        public static NavChunk[] GetSortByDistanceChunk(NavChunk[] chunks)
        {
            List<NavChunk> chunksTmp = new(chunks);
            chunksTmp.Sort(
                (x, y) => x.TargetRange.CompareTo(y.TargetRange)
                );
            return chunksTmp.ToArray();
        }
        public static NavChunk GetSortForReverseChunk(NavChunk[] chunks, int X, int Y)
        {
            List<NavChunk> chunksTmp = new(chunks);
            List<NavChunk> chunksTmpX = new();
            List<NavChunk> chunksTmpP = new();
            List<NavChunk> result = new();
            chunksTmp.ForEach(chunk =>
            {
                if (
                    (chunk.Position.X == X && chunk.Position.Y != Y) ||
                    (chunk.Position.X != X && chunk.Position.Y == Y)
                )
                {
                    chunksTmpP.Add(chunk);
                }
                else
                {
                    chunksTmpX.Add(chunk);
                }
            });
            chunksTmpP.Sort(
                (x, y) => x.StepsCount.CompareTo(y.StepsCount)
                );
            chunksTmpX.Sort(
                (x, y) => x.StepsCount.CompareTo(y.StepsCount)
                );
            if (chunksTmpP.Count == 0) return chunksTmpX.First();
            if (chunksTmpX.Count == 0) return chunksTmpP.First();
            NavChunk p = chunksTmpP.First();
            NavChunk x = chunksTmpX.First();
            if (x.StepsCount < p.StepsCount)
            {
                return x;
            }

            return p;
        }

        public static NavChunk[] GetNavChunksByPoints(Vector2Int[] points, NavChunk[,] chunks)
        {
            List<NavChunk> chunksTmp = new();
            foreach (Vector2Int item in points)
            {
                chunksTmp.Add(chunks[item.X, item.Y]);
            }
            return chunksTmp.ToArray();
        }

        // условия передвижения на чанк для навигатора
        public static bool CanMove(NavChunk start, NavChunk end)
        {
            return end.Height == 1;
        }
    }
}
