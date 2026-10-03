namespace RtsServer.App.Battle.Interfaces
{
    /// <summary>Здание потребляет энергию. Без лимита — отключается (IsPowered = false).</summary>
    public interface IEnergyConsumer
    {
        int EnergyRequired { get; }
    }
}
