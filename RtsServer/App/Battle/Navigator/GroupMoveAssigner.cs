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
            List<Unit> water = new();
            foreach (Unit u in units)
            {
                if (u == null || u.IsDestroyed) continue;
                if (u.FliesOverObstacles || !u.OccupiesGroundCell)
                    air.Add(u);
                else if (u.MovesOnWater)
                    water.Add(u);
                else
                    ground.Add(u);
            }

            foreach (Unit a in air)
                a.SetTargetPosition(click, attackMove);

            // Корабли: свой навмеш (только вода), без сухопутных слотов.
            foreach (Unit w in water)
                w.SetTargetPosition(click, attackMove);

            if (ground.Count == 0)
                return;

            Game? game = ground[0].Game;

            if (ground.Count == 1)
            {
                AssignGridPath(ground[0], click, attackMove, map, game, reservations: null);
                return;
            }

            // Как в AoW3 / SC: один коридор + полосы, свои слоты у клика. Без веера личных A*.
            if (GroupFormationPath.TryBuildAndAssign(ground, click, attackMove, map, game))
                return;

            List<Vector2Int> slots = BuildSlotsAround(click, ground.Count, map, game);
            if (slots.Count == 0)
            {
                foreach (Unit u in ground)
                    AssignGridPath(u, click, attackMove, map, game, reservations: null);
                return;
            }

            // Слоты по относительной позиции в строю, а не «кто ближе к клику».
            Vector2Float center = GroupFormationPath.ComputeSmartCenter(ground, out _);
            GetAxis(center.ToInt(), click, out _, out _, out double perpX, out double perpY);

            List<(Unit unit, double lane, double along)> ranked = ground
                .Select(u =>
                {
                    double dx = u.Position.X - center.X;
                    double dy = u.Position.Y - center.Y;
                    double lane = dx * perpX + dy * perpY;
                    double along = dx * (click.X - center.X) + dy * (click.Y - center.Y);
                    return (u, lane, along);
                })
                .OrderBy(t => t.lane)
                .ThenBy(t => t.along)
                .ThenBy(t => t.u.Id)
                .ToList();

            List<Vector2Int> slotOrder = slots
                .Select(s =>
                {
                    double dx = s.X - click.X;
                    double dy = s.Y - click.Y;
                    return (cell: s, lane: dx * perpX + dy * perpY);
                })
                .OrderBy(t => t.lane)
                .ThenBy(t => Dist2(new Vector2Float(click.X, click.Y), t.cell))
                .Select(t => t.cell)
                .ToList();

            int n = Math.Min(ranked.Count, slotOrder.Count);
            for (int i = 0; i < n; i++)
                AssignGridPath(ranked[i].unit, slotOrder[i], attackMove, map, game, reservations: null);

            for (int i = n; i < ranked.Count; i++)
                AssignGridPath(ranked[i].unit, click, attackMove, map, game, reservations: null);
        }

        private static void GetAxis(
            Vector2Int from, Vector2Int to,
            out double fwdX, out double fwdY, out double perpX, out double perpY)
        {
            fwdX = to.X - from.X;
            fwdY = to.Y - from.Y;
            double len = Math.Sqrt(fwdX * fwdX + fwdY * fwdY);
            if (len < 0.001)
            {
                fwdX = 1; fwdY = 0; perpX = 0; perpY = 1;
                return;
            }
            fwdX /= len; fwdY /= len;
            perpX = -fwdY; perpY = fwdX;
        }

        private static void AssignGridPath(
            Unit unit,
            Vector2Int dest,
            bool attackMove,
            Map map,
            Game? game,
            PathReservationMap? reservations)
        {
            // Если шаг между клетками уже начат — путь от клетки, куда едем, а не от той, что уже покинули.
            Vector2Int start = unit.GetPathStartCell();
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
