using System.Text.Json;

namespace RtsServer.Classes.Processor.Response
{
    public class MainResponse
    {
        public MainResponse(string type, string action, string status)
        {
            this.type = type;
            this.action = action;
            this.status = status;
        }

        public string type { get; set; }
        public string action { get; set; }
        public string body { get; set; }
        public string status { get; set; }

        public void SetBody<Type>(Type body)
        {
            this.body = JsonSerializer.Serialize(body);
        }
        public Type? GetBody<Type>()
        {
            return JsonSerializer.Deserialize<Type>(body);
        }

    }
}
