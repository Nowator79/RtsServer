namespace RtsServer.App.Battle.Interfaces
{
    /// <summary>Постройка с оружием: клиент крутит башню и рисует трассер по ShotSerial + Aim.</summary>
    public interface IWeaponFxSource
    {
        int ShotSerial { get; }
        float ShotAimX { get; }
        float ShotAimY { get; }
        /// <summary>Угол огневой точки по Y (как у AttackPoint юнита).</summary>
        float TurretRotation { get; }
    }
}
