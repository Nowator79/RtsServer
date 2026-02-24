using RtsServer.App.Battle.Dto;
using RtsServer.App.Battle.MapBattle;
using RtsServer.App.Battle.Units;
using RtsServer.App.FileSystem.Dto;
using RtsServer.App.NetWorkDto;
using System.Numerics;

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

        public void Start()
        {
            int tx = Unit.TargetPosition.X;
            int ty = Unit.TargetPosition.Y;

            // Проверка границ карты
            if (tx < 0 || ty < 0 || tx >= Map.Width || ty >= Map.Length)
                return;

            var targetChunk = Map.GetArrayMap()[tx, ty];
            // Блокируем перемещение на воду (Id == 0)
            if (targetChunk.Id == 0)
                return;

            // Старт маршрута: если юнит уже едет к какой-то точке — строим путь от неё, иначе от текущей позиции
            Vector2Int pathStart = Unit.GetPathStartCell();
            NavWave navWave = new(Map, pathStart, Unit.TargetPosition);
            navWave.Run();

            if (navWave.IsFail)
                return;

            Queue<Vector2Int> route = navWave.GetRoutePath();
            Vector2Int? currentWaypoint = Unit.GetCurrentWaypoint();

            // Если маршрут построен от текущей waypoint — не сбрасываем её, юнит доедет до неё и пойдёт по новому пути
            if (currentWaypoint.HasValue && route.Count > 0 && route.Peek() == currentWaypoint.Value)
                Unit.SetPathRouteFromWaypoint(route);
            else
                Unit.SetPathRoute(route);
        }

    }
}
