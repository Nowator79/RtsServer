namespace RtsServer.App.Battle.Interfaces
{
    public interface IResourceProducer
    {
        public int ResourcePerMinute { get; }
        public void ProduceResources(double deltaTime);
    }
}
