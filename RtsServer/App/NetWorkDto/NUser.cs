namespace RtsServer.App.NetWorkDto
{
    public struct NUser
    {
        public NUser(string userName, string password)
        {
            UserName = userName;
            Password = password;
        }

        public int Id { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
    }
}
