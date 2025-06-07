using RtsServer.App.Battle.Units;
using RtsServer.App.NetWorkDto;

namespace RtsServer.App.Adapters
{
    public static class UnitAdapter
    {
        public static NUnit Get(Unit unit)
        {
            NUnit unitRes = new NUnit(unit.Id, unit.Code, unit.Health.Value, unit.Position, unit.Rotation);
            unitRes.Info = new NUnit.NUnitInfo(unit.MaxSpeed, unit.CurrentSpeed);
            unitRes.AttackPoints = new NAttackPoint[unit.AttackingPoints.Length];
            for (int i = 0; i < unit.AttackingPoints.Length; i++)
            {
                Battle.Units.AttackingPoint.BaseAttackingPoint attackPoint = unit.AttackingPoints[i];
                unitRes.AttackPoints[i] = new NAttackPoint(attackPoint.Rotation);
            }
            return unitRes;
        }
    }
}
