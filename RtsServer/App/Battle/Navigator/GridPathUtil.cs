using RtsServer.App.Battle.Dto;

namespace RtsServer.App.Battle.Navigator
{
    /// <summary>Гарантирует, что маршрут идёт только по соседним клеткам (Chebyshev ≤ 1).</summary>
    public static class GridPathUtil
    {
        public static List<Vector2Int> EnsureAdjacent(IReadOnlyList<Vector2Int> path)
        {
            if (path == null || path.Count == 0)
                return new List<Vector2Int>();

            List<Vector2Int> result = new() { path[0] };
            for (int i = 1; i < path.Count; i++)
            {
                Vector2Int prev = result[^1];
                Vector2Int next = path[i];
                if (prev.Equals(next))
                    continue;

                if (Chebyshev(prev, next) > 1)
                {
                    foreach (Vector2Int step in TraceLine(prev, next))
                    {
                        if (!result[^1].Equals(step))
                            result.Add(step);
                    }
                }
                else
                {
                    result.Add(next);
                }
            }

            return result;
        }

        private static int Chebyshev(Vector2Int a, Vector2Int b) =>
            Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));

        private static IEnumerable<Vector2Int> TraceLine(Vector2Int from, Vector2Int to)
        {
            int x0 = from.X, y0 = from.Y;
            int x1 = to.X, y1 = to.Y;
            int dx = Math.Abs(x1 - x0);
            int dy = Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            while (!(x0 == x1 && y0 == y1))
            {
                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x0 += sx; }
                if (e2 < dx) { err += dx; y0 += sy; }
                yield return new Vector2Int(x0, y0);
            }
        }
    }
}
