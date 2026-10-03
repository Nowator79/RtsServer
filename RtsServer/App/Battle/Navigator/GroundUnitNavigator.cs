using RtsServer.App.Battle.Dto;
using RtsServer.App.Battle.MapBattle;
using RtsServer.App.Battle.Units;

namespace RtsServer.App.Battle.Navigator
{
    public class GroundUnitNavigator : INavigator
    {
        private Map Map;
        private Unit Unit;

        public INavigator SetMap(Map map)
        {
            Map = map;
            return this;
        }

        public INavigator SetUnit(Unit unit)
        {
            Unit = unit;
            return this;
        }

        public void Start() => Begin(retargetDestination: true, reservations: null);

        public void Start(PathReservationMap? reservations) =>
            Begin(retargetDestination: true, reservations: reservations);

        public void StartRepath() => Begin(retargetDestination: false, reservations: null);

        private void Begin(bool retargetDestination, PathReservationMap? reservations)
        {
            int tx = Unit.TargetPosition.X;
            int ty = Unit.TargetPosition.Y;

            if (tx < 0 || ty < 0 || tx >= Map.Width || ty >= Map.Length)
                return;

            var targetChunk = Map.GetArrayMap()[tx, ty];
            if (targetChunk.Id == 0)
                return;

            Vector2Int destination = Unit.TargetPosition;
            if (retargetDestination)
            {
                // Новый приказ — подобрать свободный финиш. Без «зазора 1 клетка» (он ломал колонны).
                destination = FindNearestFreeCell(Unit.TargetPosition);
                Unit.SetNavigatingDestination(destination);
            }
            else if (!IsUsableDestination(destination))
            {
                // Финиш внезапно стал непроходимым — только тогда сдвигаем.
                destination = FindNearestFreeCell(Unit.TargetPosition);
                Unit.SetNavigatingDestination(destination);
            }

            Vector2Int pathStart = Unit.GetPathStartCell();
            if (pathStart.Equals(destination))
            {
                Unit.SetPathRoute(new Queue<Vector2Int>());
                return;
            }

            Func<Vector2Int, int>? extraCost = reservations != null
                ? reservations.GetExtraCost
                : null;

            using NavWave navWave = new(Map, pathStart, destination, Unit, extraCost);
            navWave.Run();

            if (navWave.IsFail)
            {
                Unit.SetPathRoute(new Queue<Vector2Int>());
                return;
            }

            Queue<Vector2Int> route = navWave.GetRoutePath();
            List<Vector2Int> cells = GridPathUtil.EnsureAdjacent(route.ToList());
            reservations?.AddPath(cells);
            var cellQueue = new Queue<Vector2Int>(cells);

            Vector2Int? currentWaypoint = Unit.GetCurrentWaypoint();

            if (currentWaypoint.HasValue && cellQueue.Count > 0 && cellQueue.Peek() == currentWaypoint.Value)
                Unit.SetPathRouteFromWaypoint(cellQueue);
            else
                Unit.SetPathRoute(cellQueue);
        }

        private Vector2Int FindNearestFreeCell(Vector2Int desired)
        {
            if (IsFreeForUnit(desired))
                return desired;

            const int maxRadius = 48;
            var visited = new HashSet<Vector2Int> { desired };
            var queue = new Queue<(Vector2Int cell, int dist)>();
            queue.Enqueue((desired, 0));

            while (queue.Count > 0)
            {
                (Vector2Int current, int dist) = queue.Dequeue();
                if (dist >= maxRadius)
                    continue;

                foreach (Vector2Int near in NavHelper.GetSafeNear(current, Map.Width, Map.Length))
                {
                    if (!visited.Add(near))
                        continue;

                    if (IsFreeForUnit(near))
                        return near;

                    if (IsWalkableTerrain(near))
                        queue.Enqueue((near, dist + 1));
                }
            }

            return desired;
        }

        private bool IsWalkableTerrain(Vector2Int cell)
        {
            var chunk = Map.GetArrayMap()[cell.X, cell.Y];
            if (chunk.Height != 1 || chunk.Id == 0)
                return false;
            if (NavHelper.IsBlockedByConstruction(Unit?.Game, cell))
                return false;
            return true;
        }

        private bool IsUsableDestination(Vector2Int cell)
        {
            if (!IsWalkableTerrain(cell))
                return false;

            var chunk = Map.GetArrayMap()[cell.X, cell.Y];
            foreach (Unit other in chunk.UnitsInPoint)
            {
                if (other == null || other == Unit || other.IsDestroyed || !other.OccupiesGroundCell)
                    continue;
                if (other.IsParked)
                    return false;
            }

            return true;
        }

        private bool IsFreeForUnit(Vector2Int cell)
        {
            if (!IsWalkableTerrain(cell))
                return false;

            var chunk = Map.GetArrayMap()[cell.X, cell.Y];
            foreach (Unit other in chunk.UnitsInPoint)
            {
                if (other == null || other == Unit || other.IsDestroyed || !other.OccupiesGroundCell)
                    continue;
                return false;
            }

            // Резерв финиша другим едущим.
            if (Unit.Game?.Units != null)
            {
                foreach (Unit other in Unit.Game.Units)
                {
                    if (other == null || other == Unit || other.IsDestroyed || other.IsParked)
                        continue;
                    if (!other.OccupiesGroundCell)
                        continue;
                    if (other.TargetPosition.Equals(cell))
                        return false;
                }
            }

            return true;
        }
    }
}
