using RtsServer.App.Battle.MapBattle;
using RtsServer.App.Battle.Units;

namespace RtsServer.App.Battle.Navigator
{
    public interface INavigator
    {
        public INavigator SetMap(Map map);
        public INavigator SetUnit(Unit unit);
        public void Start();

    }
}
