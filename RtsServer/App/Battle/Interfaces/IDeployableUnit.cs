using RtsServer.App.Battle.Dto;

namespace RtsServer.App.Battle.Interfaces
{
    /// <summary>Юнит, который может развернуться в постройку на клетке.</summary>
    public interface IDeployableUnit
    {
        string DeployedConstructionCode { get; }
        bool CanDeployAt(Vector2Int cell);
        bool TryDeploy(Vector2Int cell);
    }
}
