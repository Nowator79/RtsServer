using RtsServer.App.Battle.MapBattle;
using RtsServer.App.Battle.Units;

namespace RtsServer.App.Battle.Navigator
{
    public interface INavigator
    {
        INavigator SetMap(Map map);
        INavigator SetUnit(Unit unit);
        /// <summary>Новый приказ: можно сдвинуть финиш на свободную клетку.</summary>
        void Start();
        /// <summary>Новый приказ с soft-резервацией коридоров (групповой ход).</summary>
        void Start(PathReservationMap? reservations);
        /// <summary>Перестроение пути к текущей TargetPosition без смены финиша (пробки).</summary>
        void StartRepath();
    }
}
