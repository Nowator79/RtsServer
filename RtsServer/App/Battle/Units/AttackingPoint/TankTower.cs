namespace RtsServer.App.Battle.Units.AttackingPoint
{
    public class TankTower(Unit CurrentUnit, float Rotation = 0) : BaseAttackingPoint(CurrentUnit, Rotation)
    {

        public override void Init()
        {
            Damage = 700; SpeedRotation = 60; Range = 10; fireCooldown = 5;
        }
    }
}
