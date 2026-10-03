using System.Text.Json.Serialization;

/// <summary>Приказ атаковать цель (юнит или постройка).</summary>
public class SetAttackTargetUnits
{
    [JsonPropertyName("UnitsIds")]
    public List<int> UnitsIds { get; set; } = new();

    /// <summary>Id цели: юнита или постройки (см. TypeTarget).</summary>
    [JsonPropertyName("TargetUnitId")]
    public int TargetUnitId { get; set; }

    /// <summary>1 = юнит, 3 = постройка.</summary>
    [JsonPropertyName("TypeTarget")]
    public int TypeTarget { get; set; }

    public SetAttackTargetUnits() { }

    public SetAttackTargetUnits(List<int> UnitsIds, int TargetUnitId, int TypeTarget)
    {
        this.UnitsIds = UnitsIds ?? new List<int>();
        this.TargetUnitId = TargetUnitId;
        this.TypeTarget = TypeTarget;
    }
}
