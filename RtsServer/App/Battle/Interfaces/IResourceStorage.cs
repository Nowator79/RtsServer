namespace RtsServer.App.Battle.Interfaces
{
    public interface IResourceStorage
    {
        float Resources { get; protected set; }
        int LimitResources { get; }
    }
}
