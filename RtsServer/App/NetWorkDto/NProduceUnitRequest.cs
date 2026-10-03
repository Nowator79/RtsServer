using System.Text.Json.Serialization;

namespace RtsServer.App.NetWorkDto
{
    public class NProduceUnitRequest
    {
        [JsonPropertyName("FactoryId")]
        public int FactoryId { get; set; }

        [JsonPropertyName("UnitCode")]
        public string UnitCode { get; set; } = "";

        public NProduceUnitRequest() { }

        public NProduceUnitRequest(int factoryId, string unitCode)
        {
            FactoryId = factoryId;
            UnitCode = unitCode ?? "";
        }
    }
}
