using RtsServer.App.Battle.Dto;
using RtsServer.App.Battle.MapBattle;
using Game = RtsServer.App.Battle.Game;

namespace RtsServer.App.Battle.Navigator
{
    /// <summary>
    /// String-pull: выкидывает лишние waypoint'ы, если между клетками есть прямая видимость.
    /// Юнит едет длинными прямыми отрезками вместо клетка-за-клеткой зигзага.
    /// </summary>
    public static class PathSmoother
    {
        public static List<Vector2Int> Smooth(IReadOnlyList<Vector2Int> path, Map map, Game? game)
        {
            if (path == null || path.Count <= 2)
                return path?.ToList() ?? new List<Vector2Int>();

            List<Vector2Int> result = new() { path[0] };
            int i = 0;
            while (i < path.Count - 1)
            {
                int best = i + 1;
                for (int j = path.Count - 1; j > i + 1; j--)
                {
                    if (HasLineOfSight(path[i], path[j], map, game))
                    {
                        best = j;
                        break;
                    }
                }

                result.Add(path[best]);
                i = best;
            }

            return result;
        }

        /// <summary>Зазор прямой: диаметр техники 0.5, чтобы срез угла не втыкал корпус в здание.</summary>
        private const double PathClearance = 0.25;

        /// <summary>Все клетки суперкавера линии проходимы, и корпус не задевает угол.</summary>
        public static bool HasLineOfSight(Vector2Int from, Vector2Int to, Map map, Game? game)
        {
            int prevHeight = map.GetArrayMap()[from.X, from.Y].Height;
            foreach (Vector2Int cell in SupercoverLine(from, to))
            {
                if (!NavHelper.IsTerrainWalkable(map, game, cell))
                    return false;
                int height = map.GetArrayMap()[cell.X, cell.Y].Height;
                if (!NavHelper.CanStepHeight(prevHeight, height))
                    return false;
                prevHeight = height;
            }

            return SegmentClearsBody(from, to, map, game);
        }

        /// <summary>Точки отрезка между центрами клеток не проникают в блок кругом техники.</summary>
        private static bool SegmentClearsBody(Vector2Int from, Vector2Int to, Map map, Game? game)
        {
            var tiles = map.GetArrayMap();
            int width = tiles.GetLength(0);
            int length = tiles.GetLength(1);
            double dx = to.X - from.X;
            double dy = to.Y - from.Y;
            double len = Math.Sqrt(dx * dx + dy * dy);
            int steps = Math.Max(1, (int)Math.Ceiling(len / 0.1));
            for (int i = 0; i <= steps; i++)
            {
                double t = i / (double)steps;
                double x = from.X + dx * t;
                double y = from.Y + dy * t;
                int cx = NavHelper.CenteredCell(x);
                int cy = NavHelper.CenteredCell(y);
                if (cx < 0 || cy < 0 || cx >= width || cy >= length || tiles[cx, cy] == null)
                    return false;
                int height = tiles[cx, cy].Height;
                if (NavHelper.CircleHitsBlocked(map, game, x, y, PathClearance, height))
                    return false;
            }

            return true;
        }

        /// <summary>Клетки, которые пересекает отрезок (включая концы).</summary>
        public static IEnumerable<Vector2Int> SupercoverLine(Vector2Int from, Vector2Int to)
        {
            int x0 = from.X, y0 = from.Y;
            int x1 = to.X, y1 = to.Y;
            int dx = Math.Abs(x1 - x0);
            int dy = Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;

            yield return new Vector2Int(x0, y0);

            if (dx == 0 && dy == 0)
                yield break;

            // Amanatides & Woo style grid walk.
            double x = x0 + 0.5;
            double y = y0 + 0.5;
            double xEnd = x1 + 0.5;
            double yEnd = y1 + 0.5;
            double vx = xEnd - x;
            double vy = yEnd - y;
            double len = Math.Sqrt(vx * vx + vy * vy);
            if (len < 1e-9)
                yield break;

            vx /= len;
            vy /= len;

            int ix = x0, iy = y0;
            double tMaxX = vx > 0 ? ((ix + 1) - x) / vx : (vx < 0 ? (ix - x) / vx : double.PositiveInfinity);
            double tMaxY = vy > 0 ? ((iy + 1) - y) / vy : (vy < 0 ? (iy - y) / vy : double.PositiveInfinity);
            double tDeltaX = vx != 0 ? Math.Abs(1.0 / vx) : double.PositiveInfinity;
            double tDeltaY = vy != 0 ? Math.Abs(1.0 / vy) : double.PositiveInfinity;
            int stepX = vx > 0 ? 1 : (vx < 0 ? -1 : 0);
            int stepY = vy > 0 ? 1 : (vy < 0 ? -1 : 0);

            double travelled = 0;
            int guard = dx + dy + 4;
            while (guard-- > 0 && (ix != x1 || iy != y1))
            {
                if (tMaxX < tMaxY)
                {
                    travelled = tMaxX;
                    tMaxX += tDeltaX;
                    ix += stepX;
                }
                else if (tMaxY < tMaxX)
                {
                    travelled = tMaxY;
                    tMaxY += tDeltaY;
                    iy += stepY;
                }
                else
                {
                    // Ровно через угол — заходим в обе соседние, чтобы не «срезать» блок.
                    travelled = tMaxX;
                    tMaxX += tDeltaX;
                    tMaxY += tDeltaY;
                    ix += stepX;
                    iy += stepY;
                }

                yield return new Vector2Int(ix, iy);
                if (travelled > len + 0.01)
                    break;
            }
        }
    }
}
