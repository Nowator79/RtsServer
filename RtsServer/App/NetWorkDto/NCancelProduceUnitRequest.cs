using System.Text.Json.Serialization;

namespace RtsServer.App.NetWorkDto
{
    public class NCancelProduceUnitRequest
    {
        [JsonPropertyName("FactoryId")]
        public int FactoryId { get; set; }

        [JsonPropertyName("UnitCode")]
        public string UnitCode { get; set; } = "";

        public NCancelProduceUnitRequest() { }

        public NCancelProduceUnitRequest(int factoryId, string unitCode)
        {
            FactoryId = factoryId;
            UnitCode = unitCode ?? "";
        }
    }
}
