using RtsServer.App.Battle.Dto;

public class SetAttackTargetUnits
{
    public List<int> UnitsIds { get; set; }
    public int TargetUnitId { get; set; }
    public int TypeTarget { get; set; }

    public SetAttackTargetUnits(List<int> UnitsIds, int TargetUnitId, int TypeTarget)
    {
        this.UnitsIds = UnitsIds;
        this.TargetUnitId = TargetUnitId;
        this.TypeTarget = TypeTarget;
    }
}
