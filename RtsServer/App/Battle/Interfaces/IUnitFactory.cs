namespace RtsServer.App.Battle.Interfaces
{
    /// <summary>Постройка, способная производить юнитов.</summary>
    public interface IUnitFactory
    {
        bool CanProduce(string unitCode);
        bool Enqueue(string unitCode, float buildSeconds);
        bool TryGetRecipe(string unitCode, out int cost, out float buildSeconds);
    }
}
