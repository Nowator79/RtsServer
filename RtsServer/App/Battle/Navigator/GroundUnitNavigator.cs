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

        public async void Start()
        {
            // Блокируем перемещение на воду
            if (Map.GetArrayMap()[Unit.TargetPosition.X, Unit.TargetPosition.Y].Id == 0)
                return;

            // Чистим предыдущий маршрут
            Unit.PathRoute?.Clear();

            Vector2Int curPosition = Unit.Position.ToInt();
            NavWave navWave = new(Map, curPosition, Unit.TargetPosition);
            navWave.Run();

            // Если путь найден — отправляем, но в правильном порядке
            if (!navWave.IsFail)
            {
                await Unit.UpdatePathRouteAsync(navWave.GetRoutePath());
            }
        }

    }
}
