using System.Text.Json.Serialization;
using RtsServer.App.Battle.Dto;

public class SetTargetUnits
{
    [JsonPropertyName("UnitsIds")]
    public List<int> UnitsIds { get; set; } = new();

    [JsonPropertyName("Target")]
    public Vector2Int Target { get; set; }

    [JsonPropertyName("TypeTarget")]
    public int TypeTarget { get; set; }

    public SetTargetUnits() { }

    public SetTargetUnits(List<int> UnitsIds, Vector2Int Target, int TypeTarget)
    {
        this.UnitsIds = UnitsIds ?? new List<int>();
        this.Target = Target;
        this.TypeTarget = TypeTarget;
    }
}
