namespace RtsServer.App.NetWorkDto
{
    public struct NAuthFail
    {
        public string Message { get; set; }

        public NAuthFail(string message)
        {
            Message = message;
        }
    }
}
