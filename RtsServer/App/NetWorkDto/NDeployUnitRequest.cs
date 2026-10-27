using System.Text.Json.Serialization;
using RtsServer.App.Battle.Dto;

namespace RtsServer.App.NetWorkDto
{
    public class NDeployUnitRequest
    {
        [JsonPropertyName("UnitId")]
        public int UnitId { get; set; }

        [JsonPropertyName("X")]
        public int X { get; set; }

        [JsonPropertyName("Y")]
        public int Y { get; set; }

        public NDeployUnitRequest() { }

        public NDeployUnitRequest(int unitId, Vector2Int cell)
        {
            UnitId = unitId;
            X = cell.X;
            Y = cell.Y;
        }
    }
}
