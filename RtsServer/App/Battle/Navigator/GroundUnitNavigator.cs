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
            bool fly = Unit?.FliesOverObstacles ?? false;
            bool water = Unit?.MovesOnWater ?? false;
            // Вода запрещена наземным (кроме клика→ближайшая вода для кораблей). Воздух — куда угодно.
            if (!water && !fly && targetChunk.Id == 0)
                return;

            Vector2Int destination = Unit.TargetPosition;
            if (retargetDestination)
            {
                // Корабль: клик по суше → ближайшая вода. Наземный: свободная клетка у цели.
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
                // Объезд пробки не должен стирать уже идущий маршрут.
                if (!retargetDestination && Unit.HasActiveRoute)
                    return;
                ApplyRoute(new Queue<Vector2Int>());
                return;
            }

            Func<Vector2Int, int>? extraCost = reservations != null
                ? reservations.GetExtraCost
                : null;

            bool flyNav = Unit?.FliesOverObstacles ?? false;
            bool waterNav = Unit?.MovesOnWater ?? false;
            using NavWave navWave = new(
                Map, pathStart, destination, Unit, extraCost,
                flyOverObstacles: flyNav, waterOnly: waterNav);
            navWave.Run();

            if (navWave.IsFail)
            {
                if (!retargetDestination && Unit.HasActiveRoute)
                    return;
                ApplyRoute(new Queue<Vector2Int>());
                return;
            }

            Queue<Vector2Int> route = navWave.GetRoutePath();
            List<Vector2Int> cells = GridPathUtil.EnsureAdjacent(route.ToList());
            cells = PathSmoother.Smooth(
                cells, Map, Unit.Game,
                ignoreObstacles: flyNav || waterNav,
                canTraverseForest: flyNav || (Unit?.CanTraverseForest ?? false));
            reservations?.AddPath(cells);
            ApplyRoute(new Queue<Vector2Int>(cells));
        }

        /// <summary>Новый приказ заменяет маршрут целиком. Старую точку пути не доезжаем.</summary>
        private void ApplyRoute(Queue<Vector2Int> route)
        {
            Vector2Int here = Unit.Position.ToInt();
            while (route.Count > 0 && route.Peek().Equals(here))
                route.Dequeue();

            Unit.SetPathRoute(route);
        }

        private Vector2Int FindNearestFreeCell(Vector2Int desired)
        {
            if (IsFreeForUnit(desired))
                return desired;

            bool waterUnit = Unit?.MovesOnWater == true;
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

                    // Корабль: flood-fill по любой клетке, пока не найдём воду.
                    // Земля: только по проходимому рельефу.
                    if (waterUnit || IsWalkableTerrain(near))
                        queue.Enqueue((near, dist + 1));
                }
            }

            return desired;
        }

        private bool IsWalkableTerrain(Vector2Int cell)
        {
            if (Unit?.FliesOverObstacles == true)
                return true;

            var chunk = Map.GetArrayMap()[cell.X, cell.Y];
            if (Unit?.MovesOnWater == true)
                return chunk != null && chunk.Id == 0;

            if (!NavHelper.IsWalkableHeight(chunk))
                return false;
            if (NavHelper.IsBlockedByConstruction(Unit?.Game, cell))
                return false;
            if (!(Unit?.CanTraverseForest ?? false) && NavHelper.IsBlockedByForest(Map, cell.X, cell.Y))
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
                if (!other.IsParked)
                    continue;
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
                // Финишный слот — только если клетка реально свободна.
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
