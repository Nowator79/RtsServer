using RtsServer.App.Buttle.MapButlle;
using RtsServer.App.Buttle.Units;

namespace RtsServer.App.Buttle.Navigator
{
    public interface INavigator
    {
        public INavigator SetMap(Map map);
        public INavigator SetUnit(Unit unit);
        public void Start();

    }
}
