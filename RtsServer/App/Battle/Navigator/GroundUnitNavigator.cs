using RtsServer.App.Battle.Dto;
using RtsServer.App.Battle.MapButlle;
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
            /// пока блокируем перемещние на водные клетки, потом надо 
            /// будет сделать функцию ближайшей доступной точки на суше
            if (Map.GetArrayMap()[Unit.TargetPosition.X, Unit.TargetPosition.Y].Id == 0) return;

            // чистим буффер
            if (Unit.PathRout != null)
            {
                Unit.PathRout.Clear();
            }

            Vector2Int curPosition = Unit.Position.ToInt();
            NavWave navWave = new(Map, curPosition, Unit.TargetPosition);
            navWave.Run();

            // выставляем результат функции поиска пути волной 
            if (!navWave.IsFail)
            {
                Unit.SetRouts(navWave.GetRoutPath().ToHashSet());
            }
        }
    }
}
