using RtsServer.App.Buttle.Dto;
using System.Collections.Generic;

namespace RtsServer.App.Buttle.Navigator
{
    public static class NavHelper
    {
        public static float DistanceSQRT(Vector2Int start, Vector2Int end)
        {
            return (float)(Math.Pow(end.X - start.X, 2) + Math.Pow(end.Y - start.Y, 2));
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
            HashSet<Vector2Int> result = new(GetNear(vector2Int));
            result.RemoveWhere(item => item.X < 0 || item.Y < 0 || item.X >= width || item.Y >= length);
            return result.ToArray();
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
