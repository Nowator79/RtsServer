namespace RtsServer.App.Battle.Interfaces
{
    /// <summary>Класс цели для оружия (можно комбинировать флагами).</summary>
    [Flags]
    public enum AttackTargetDomain
    {
        None = 0,
        Ground = 1 << 0,
        Air = 1 << 1,
        Construction = 1 << 2,

        /// <summary>Всё, что обычно бьёт наземная турель.</summary>
        Surface = Ground | Construction,
        All = Ground | Air | Construction
    }
}
