using System.Text.Json.Serialization;
using RtsServer.App.Battle.Dto;

namespace RtsServer.App.NetWorkDto
{
    public class NBuildConstructionRequest
    {
        [JsonPropertyName("Code")]
        public string Code { get; set; } = "";

        [JsonPropertyName("Position")]
        public Vector2Int Position { get; set; }
    }
}
