using System.Text.Json.Serialization;

namespace RtsServer.App.NetWorkDto
{
    public class NUndeployConstructionRequest
    {
        [JsonPropertyName("ConstructionId")]
        public int ConstructionId { get; set; }

        public NUndeployConstructionRequest() { }

        public NUndeployConstructionRequest(int constructionId)
        {
            ConstructionId = constructionId;
        }
    }
}
