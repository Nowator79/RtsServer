namespace RtsServer.App.Battle.Interfaces
{
    /// <summary>Постройка, которая сворачивается обратно в мобильный юнит.</summary>
    public interface IUndeployableConstruction
    {
        string PackedUnitCode { get; }
        bool TryUndeploy();
    }
}
