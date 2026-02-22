namespace RtsServer.App.NetWorkDto
{
    public class NMapRequest
    {
        public NMapRequest(string code)
        {
            Code = code;
        }

        public string Code { get; set; }
    }
}
