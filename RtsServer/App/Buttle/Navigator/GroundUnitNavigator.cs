using RtsServer.App.Buttle.Dto;
using RtsServer.App.Buttle.MapButlle;
using RtsServer.App.Buttle.Units;
using RtsServer.App.FileSystem.Dto;
using RtsServer.App.NetWorkDto;
using System.Numerics;

namespace RtsServer.App.Buttle.Navigator
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
            if (Unit.PathRout != null)
            {
                Unit.PathRout.Clear();
            }
            Vector2Int curPosition = Unit.Position.ToInt();
            NavWave navWave = new(Map, curPosition, Unit.TargetPosition);
            navWave.Run();
            Unit.SetRouts(navWave.GetRoutPath().ToHashSet());
        }
    }
}
