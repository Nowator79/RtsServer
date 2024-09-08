using RtsServer.App.Battle.Units;
using RtsServer.App.NetWorkDto;

namespace RtsServer.App.Adapters
{
    public static class UnitAdapter
    {
        public static NUnit Get(Unit unit)
        {
            return new NUnit(unit.Id, unit.Code, unit.Health.Value, unit.Position, unit.Rotation);
        }
    }
}
