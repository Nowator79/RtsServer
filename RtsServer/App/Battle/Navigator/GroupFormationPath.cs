using RtsServer.App.Battle.Dto;
using RtsServer.App.Battle.MapBattle;
using RtsServer.App.Battle.Units;
using Game = RtsServer.App.Battle.Game;

namespace RtsServer.App.Battle.Navigator
{
    /// <summary>
    /// Один spine + фиксированная боковая полоса + непрерывный путь (без прыжков клеток).
    /// </summary>
    public static class GroupFormationPath
    {
        private const int CornerPushCells = 1;
        private const int MaxLane = 3;

        public static bool TryBuildAndAssign(
            IReadOnlyList<Unit> ground,
            Vector2Int click,
            bool attackMove,
            Map map,
            Game? game)
        {
            if (ground == null || ground.Count < 2 || map == null)
                return false;

            Vector2Float center = ComputeSmartCenter(ground, out _);
            Vector2Int start = NavHelper.FindNearestWalkable(map, game, center.ToInt());
            Vector2Int end = NavHelper.FindNearestWalkable(map, game, click);

            List<Vector2Int>? spine = BuildSpinePath(map, game, ground[0], start, end);
            if (spine == null || spine.Count == 0)
                return false;

            spine = PushCornersOffObstacles(spine, map, game);
            spine = EnsureAdjacentPath(spine, map, game);
            if (spine.Count == 0)
                return false;

            GetTravelAxis(start, end, out _, out _, out double perpX, out double perpY);

            List<Vector2Int> slots = BuildSlotsAround(click, ground.Count, map, game);
            if (slots.Count == 0)
                return false;

            HashSet<int> takenSlot = new();
            List<Unit> order = ground
                .OrderBy(u => Dist2(u.Position, click))
                .ThenBy(u => u.Id)
                .ToList();

            int assigned = 0;
            foreach (Unit unit in order)
            {
                int slotIndex = PickNearestSlot(unit, slots, takenSlot);
                Vector2Int dest = slotIndex < 0 ? end : slots[slotIndex];
                if (slotIndex >= 0)
                    takenSlot.Add(slotIndex);

                double dx = unit.Position.X - center.X;
                double dy = unit.Position.Y - center.Y;
                int lane = (int)Math.Round(dx * perpX + dy * perpY);
                if (lane > MaxLane) lane = MaxLane;
                if (lane < -MaxLane) lane = -MaxLane;

                List<Vector2Int> personal = BuildLanePath(
                    spine, lane, unit.Position.ToInt(), dest, map, game);

                if (personal.Count == 0)
                    continue;

                unit.ApplyPreparedRoute(dest, personal, attackMove);
                assigned++;
            }

            return assigned > 0;
        }

        private static List<Vector2Int> BuildLanePath(
            List<Vector2Int> spine,
            int lane,
            Vector2Int unitStart,
            Vector2Int finalDest,
            Map map,
            Game? game)
        {
            List<Vector2Int> lanePath = new(spine.Count);
            for (int i = 0; i < spine.Count; i++)
            {
                GetLocalPerp(spine, i, out double px, out double py);
                int ox = (int)Math.Round(px * lane);
                int oy = (int)Math.Round(py * lane);
                Vector2Int candidate = new(spine[i].X + ox, spine[i].Y + oy);
                if (!NavHelper.IsTerrainWalkable(map, game, candidate))
                    candidate = spine[i];
                if (lanePath.Count == 0 || !lanePath[^1].Equals(candidate))
                    lanePath.Add(candidate);
            }

            List<Vector2Int> full = new() { unitStart };
            foreach (Vector2Int cell in lanePath)
                ConnectTo(full, cell, map, game);
            ConnectTo(full, finalDest, map, game);
            return EnsureAdjacentPath(full, map, game);
        }

        private static void ConnectTo(List<Vector2Int> path, Vector2Int next, Map map, Game? game)
        {
            if (path.Count == 0)
            {
                path.Add(next);
                return;
            }

            Vector2Int prev = path[^1];
            if (prev.Equals(next))
                return;

            if (Chebyshev(prev, next) > 1)
            {
                foreach (Vector2Int step in TraceLine(prev, next))
                {
                    if (path[^1].Equals(step))
                        continue;
                    Vector2Int cell = step;
                    if (!NavHelper.IsTerrainWalkable(map, game, cell) && !cell.Equals(next))
                        cell = NavHelper.FindNearestWalkable(map, game, cell, 2);
                    if (!path[^1].Equals(cell))
                        path.Add(cell);
                }
            }

            if (!path[^1].Equals(next))
                path.Add(next);
        }

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

        private static List<Vector2Int> EnsureAdjacentPath(List<Vector2Int> path, Map map, Game? game)
        {
            if (path.Count <= 1)
                return path;

            List<Vector2Int> result = new() { path[0] };
            for (int i = 1; i < path.Count; i++)
                ConnectTo(result, path[i], map, game);
            return result;
        }

        private static int Chebyshev(Vector2Int a, Vector2Int b) =>
            Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));

        private static void GetTravelAxis(
            Vector2Int start, Vector2Int end,
            out double fwdX, out double fwdY, out double perpX, out double perpY)
        {
            fwdX = end.X - start.X;
            fwdY = end.Y - start.Y;
            double len = Math.Sqrt(fwdX * fwdX + fwdY * fwdY);
            if (len < 0.001)
            {
                fwdX = 1; fwdY = 0; perpX = 0; perpY = 1;
                return;
            }
            fwdX /= len; fwdY /= len;
            perpX = -fwdY; perpY = fwdX;
        }

        private static List<Vector2Int>? BuildSpinePath(
            Map map, Game? game, Unit probe, Vector2Int start, Vector2Int end)
        {
            if (start.Equals(end))
                return new List<Vector2Int> { start };

            using NavWave nav = new(map, start, end, probe);
            nav.Run();
            if (nav.IsFail) return null;
            Queue<Vector2Int> q = nav.GetRoutePath();
            return q.Count == 0 ? null : q.ToList();
        }

        public static List<Vector2Int> PushCornersOffObstacles(List<Vector2Int> path, Map map, Game? game)
        {
            if (path.Count < 3) return path;
            List<Vector2Int> result = new(path);
            for (int i = 1; i < result.Count - 1; i++)
            {
                Vector2Int a = result[i - 1], b = result[i], c = result[i + 1];
                int abx = b.X - a.X, aby = b.Y - a.Y;
                int bcx = c.X - b.X, bcy = c.Y - b.Y;
                if (abx * bcy - aby * bcx == 0) continue;

                int px = -(aby + bcy), py = abx + bcx;
                if (px == 0 && py == 0) { px = -aby; py = abx; }
                px = Math.Sign(px); py = Math.Sign(py);
                if (px == 0 && py == 0) continue;

                bool hitPos = SideHasObstacle(b, px, py, map, game);
                bool hitNeg = SideHasObstacle(b, -px, -py, map, game);
                int pushX, pushY;
                if (hitPos && !hitNeg) { pushX = -px; pushY = -py; }
                else if (hitNeg && !hitPos) { pushX = px; pushY = py; }
                else continue;

                Vector2Int shifted = b;
                for (int step = 1; step <= CornerPushCells; step++)
                {
                    Vector2Int candidate = new(b.X + pushX * step, b.Y + pushY * step);
                    if (!NavHelper.IsTerrainWalkable(map, game, candidate)) break;
                    shifted = candidate;
                }
                result[i] = shifted;
            }
            return result;
        }

        private static bool SideHasObstacle(Vector2Int from, int dirX, int dirY, Map map, Game? game)
        {
            for (int step = 1; step <= 2; step++)
            {
                if (!NavHelper.IsTerrainWalkable(map, game, from.X + dirX * step, from.Y + dirY * step))
                    return true;
            }
            return false;
        }

        private static void GetLocalPerp(List<Vector2Int> spine, int index, out double px, out double py)
        {
            Vector2Int a = index > 0 ? spine[index - 1] : spine[index];
            Vector2Int b = index < spine.Count - 1 ? spine[index + 1] : spine[index];
            double fx = b.X - a.X, fy = b.Y - a.Y;
            double len = Math.Sqrt(fx * fx + fy * fy);
            if (len < 0.001) { px = 0; py = 1; return; }
            fx /= len; fy /= len;
            px = -fy; py = fx;
        }

        public static Vector2Float ComputeSmartCenter(IReadOnlyList<Unit> units, out double stdDev)
        {
            double ax = 0, ay = 0;
            int n = units.Count;
            if (n == 0) { stdDev = 0; return Vector2Float.Zero; }
            foreach (Unit u in units) { ax += u.Position.X; ay += u.Position.Y; }
            ax /= n; ay /= n;

            double variance = 0;
            foreach (Unit u in units)
            {
                double dx = u.Position.X - ax, dy = u.Position.Y - ay;
                variance += dx * dx + dy * dy;
            }
            stdDev = Math.Sqrt(variance / n);
            double threshold = Math.Max(1.0, stdDev);

            double sx = 0, sy = 0; int sn = 0;
            foreach (Unit u in units)
            {
                double dx = u.Position.X - ax, dy = u.Position.Y - ay;
                if (Math.Sqrt(dx * dx + dy * dy) <= threshold)
                {
                    sx += u.Position.X; sy += u.Position.Y; sn++;
                }
            }
            return sn == 0 ? new Vector2Float(ax, ay) : new Vector2Float(sx / sn, sy / sn);
        }

        private static List<Vector2Int> BuildSlotsAround(Vector2Int click, int count, Map map, Game? game)
        {
            List<Vector2Int> result = new(count);
            HashSet<Vector2Int> seen = new();
            void TryAdd(Vector2Int cell)
            {
                if (result.Count >= count) return;
                if (!seen.Add(cell)) return;
                if (!NavHelper.IsTerrainWalkable(map, game, cell)) return;
                result.Add(cell);
            }

            TryAdd(click);
            for (int ring = 1; result.Count < count && ring < 24; ring++)
            {
                for (int dx = -ring; dx <= ring; dx++)
                {
                    TryAdd(new Vector2Int(click.X + dx, click.Y - ring));
                    TryAdd(new Vector2Int(click.X + dx, click.Y + ring));
                }
                for (int dy = -ring + 1; dy <= ring - 1; dy++)
                {
                    TryAdd(new Vector2Int(click.X - ring, click.Y + dy));
                    TryAdd(new Vector2Int(click.X + ring, click.Y + dy));
                }
            }
            return result;
        }

        private static int PickNearestSlot(Unit unit, List<Vector2Int> slots, HashSet<int> taken)
        {
            int best = -1, bestDist = int.MaxValue;
            for (int i = 0; i < slots.Count; i++)
            {
                if (taken.Contains(i)) continue;
                int d = Dist2(unit.Position, slots[i]);
                if (d < bestDist) { bestDist = d; best = i; }
            }
            return best;
        }

        private static int Dist2(Vector2Float pos, Vector2Int cell)
        {
            double dx = pos.X - cell.X, dy = pos.Y - cell.Y;
            return (int)(dx * dx + dy * dy);
        }
    }
}
