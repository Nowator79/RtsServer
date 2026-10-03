using RtsServer.App.Battle.Dto;
using RtsServer.App.Battle.MapBattle;
using RtsServer.App.Battle.MapBattle.ChunksType;
using RtsServer.App.Battle.Units;
using Game = RtsServer.App.Battle.Game;

namespace RtsServer.App.Battle.Navigator
{
    /// <summary>
    /// Групповой ход: слоты у клика + личный путь по клеткам (соседние клетки, без «полёта» по миру).
    /// </summary>
    public static class GroupMoveAssigner
    {
        public static void IssueMove(IReadOnlyList<Unit> units, Vector2Int click, bool attackMove, Map map)
        {
            if (units == null || units.Count == 0 || map == null)
                return;

            List<Unit> ground = new();
            List<Unit> air = new();
            foreach (Unit u in units)
            {
                if (u == null || u.IsDestroyed) continue;
                if (u.OccupiesGroundCell) ground.Add(u);
                else air.Add(u);
            }

            foreach (Unit a in air)
                a.SetTargetPosition(click, attackMove);

            if (ground.Count == 0)
                return;

            if (ground.Count == 1)
            {
                AssignGridPath(ground[0], click, attackMove, map, ground[0].Game, reservations: null);
                return;
            }

            Game? game = ground[0].Game;
            List<Vector2Int> slots = BuildSlotsAround(click, ground.Count, map, game);
            if (slots.Count == 0)
            {
                foreach (Unit u in ground)
                    AssignGridPath(u, click, attackMove, map, game, reservations: null);
                return;
            }

            HashSet<int> takenSlot = new();
            List<Unit> order = ground
                .OrderBy(u => Dist2(u.Position, click))
                .ThenBy(u => u.Id)
                .ToList();

            PathReservationMap reservations = new();

            foreach (Unit unit in order)
            {
                int bestIndex = -1;
                int bestDist = int.MaxValue;
                for (int i = 0; i < slots.Count; i++)
                {
                    if (takenSlot.Contains(i)) continue;
                    int d = Dist2(unit.Position, slots[i]);
                    if (d < bestDist)
                    {
                        bestDist = d;
                        bestIndex = i;
                    }
                }

                Vector2Int dest = bestIndex < 0 ? click : slots[bestIndex];
                if (bestIndex >= 0)
                    takenSlot.Add(bestIndex);

                AssignGridPath(unit, dest, attackMove, map, game, reservations);
            }
        }

        private static void AssignGridPath(
            Unit unit,
            Vector2Int dest,
            bool attackMove,
            Map map,
            Game? game,
            PathReservationMap? reservations)
        {
            Vector2Int start = unit.Position.ToInt();
            dest = NavHelper.FindNearestWalkable(map, game, dest);

            if (start.Equals(dest))
            {
                unit.ApplyPreparedRoute(dest, new[] { dest }, attackMove);
                return;
            }

            Func<Vector2Int, int>? extra = reservations != null ? reservations.GetExtraCost : null;
            using NavWave nav = new(map, start, dest, unit, extra);
            nav.Run();

            if (nav.IsFail)
            {
                unit.SetTargetPosition(dest, attackMove, reservations);
                return;
            }

            // Полный путь по соседним клеткам — без string-pull «через воздух».
            List<Vector2Int> cells = GridPathUtil.EnsureAdjacent(nav.GetRoutePath().ToList());
            reservations?.AddPath(cells);
            unit.ApplyPreparedRoute(dest, cells, attackMove);
        }

        private static List<Vector2Int> BuildSlotsAround(Vector2Int click, int count, Map map, Game? game)
        {
            List<Vector2Int> result = new(count);
            HashSet<Vector2Int> seen = new();

            void TryAdd(Vector2Int cell)
            {
                if (result.Count >= count) return;
                if (!seen.Add(cell)) return;
                if (!IsClaimable(cell, map, game)) return;
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

        private static bool IsClaimable(Vector2Int cell, Map map, Game? game)
        {
            if (!NavHelper.IsTerrainWalkable(map, game, cell))
                return false;

            ChunkBase chunk = map.GetArrayMap()[cell.X, cell.Y];
            if (chunk?.UnitsInPoint == null)
                return true;

            foreach (Unit other in chunk.UnitsInPoint)
            {
                if (other == null || other.IsDestroyed || !other.OccupiesGroundCell)
                    continue;
                if (other.IsParked)
                    return false;
            }

            return true;
        }

        private static int Dist2(Vector2Float pos, Vector2Int cell)
        {
            double dx = pos.X - cell.X;
            double dy = pos.Y - cell.Y;
            return (int)(dx * dx + dy * dy);
        }
    }
}
