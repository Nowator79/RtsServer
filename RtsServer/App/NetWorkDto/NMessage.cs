namespace RtsServer.App.NetWorkDto
{
    public struct NMessage
    {
        public int UserId { get; set; }
        public string Message { get; set; }
        public NMessage(int UserId, string Message)
        {
            this.UserId = UserId;
            this.Message = Message;
        }
    }
}
