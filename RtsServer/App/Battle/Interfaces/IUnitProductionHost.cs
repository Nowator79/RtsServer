namespace RtsServer.App.Battle.Interfaces
{
    /// <summary>Завод с очередью производства — для сетевого синка прогресса.</summary>
    public interface IUnitProductionHost : IUnitFactory
    {
        float ProductionProgress { get; }
        string? CurrentProductionCode { get; }
        string ProductionQueueCsv { get; }
    }
}
