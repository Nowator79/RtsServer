using RtsServer.App.Buttle.Dto;
using RtsServer.App.NetWorkDto.Response;

public class SetTargetUnits
{
    public List<int> UnitsIds { get; set; }
    public Vector2Int Target { get; set; }
    public int TypeTarget { get; set; }


    public SetTargetUnits(List<int> UnitsIds, Vector2Int Target, int TypeTarget)
    {
        this.UnitsIds = UnitsIds;
        this.Target = Target;
        this.TypeTarget = TypeTarget;
    }
}
