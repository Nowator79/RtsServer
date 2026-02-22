namespace RtsServer.App.Battle.Interfaces
{
    public interface IUnitFactory
    {
        bool CanProduce(Type type);
        void EnqueueProduction(Type type);
        void UpdateProduction(float deltaTime);
    }
}
