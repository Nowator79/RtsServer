namespace RtsServer.App.Battle.Chat
{
    public struct Message
    {
        public string message;
        public User user;
        public DateTime dateTime;

        public Message(string message, User user)
        {
            this.message = message;
            this.user = user;
            this.dateTime = DateTime.Now;
        }
    }
}
