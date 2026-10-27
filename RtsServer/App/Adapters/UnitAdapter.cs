using RtsServer.App.Battle.Units;
using RtsServer.App.NetWorkDto;

namespace RtsServer.App.Adapters
{
    public static class UnitAdapter
    {
        public static NUnit Get(Unit unit)
        {
            NUnit unitRes = new NUnit(unit.Id, unit.Code, unit.Health.Value, unit.Position, unit.Rotation, unit.OwnerId);
            unitRes.MaxHealth = unit.Health.Max;
            // Высота полёта / отображения: у самолётов совпадает с точкой выстрела снарядов.
            // Воздух / вертолёт: клиент рисует на высоте GetBodyHeight.
            unitRes.Height = !unit.OccupiesGroundCell ? (float)unit.GetBodyHeight() : 0f;
            unitRes.Info = new NUnit.NUnitInfo(unit.MaxSpeed, unit.CurrentSpeed);
            unitRes.IsLowFlight = unit is Helicopter heli && heli.IsLowFlight;
            unitRes.Fuel01 = unit is AirUnit air ? air.Fuel01 : 1f;
            unitRes.AttackPoints = new NAttackPoint[unit.AttackingPoints.Length];
            for (int i = 0; i < unit.AttackingPoints.Length; i++)
            {
                Battle.Units.AttackingPoint.BaseAttackingPoint attackPoint = unit.AttackingPoints[i];
                unitRes.AttackPoints[i] = new NAttackPoint(
                    attackPoint.Rotation,
                    attackPoint.ShotSerial,
                    attackPoint.LastShotAimX,
                    attackPoint.LastShotAimY);
            }
            return unitRes;
        }
    }
}
