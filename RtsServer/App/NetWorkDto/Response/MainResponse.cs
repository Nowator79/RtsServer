using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RtsServer.App.NetWorkDto.Response
{
    public class MainResponse
    {
        private static readonly JsonSerializerOptions BodyJsonOptions = new()
        {
            // Unity JsonUtility плохо переваривает \u0022 из default encoder.
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        [JsonPropertyName("Type")]
        public string Type { get; set; }

        [JsonPropertyName("Action")]
        public string Action { get; set; }

        [JsonPropertyName("Status")]
        public string Status { get; set; }

        [JsonIgnore]
        public object BodyObject { get; private set; }
        [JsonPropertyName("Body")]
        public string Body { get; private set; }


        public MainResponse(string Type, string Action, string Body, string Status)
        {
            this.Type = Type;
            this.Action = Action;
            this.Body = Body;
            this.Status = Status;
        }


        public MainResponse SetBody<T>(T body)
        {
            if (body == null)
            {
                Body = "";
                return this;
            }

            Body = JsonSerializer.Serialize(body, body.GetType(), BodyJsonOptions);
            return this;
        }

        public T GetBody<T>()
        {
            return JsonSerializer.Deserialize<T>(Body) ?? throw new JsonException();
        }

    }
}
