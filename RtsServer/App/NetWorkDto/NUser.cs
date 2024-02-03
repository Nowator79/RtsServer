using RtsServer.App.NetWorkDto.Response;

namespace RtsServer.App.NetWorkDto
{
    public struct NUser
    {
       
        public int Id { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public int Credits { get; set; }
        public int TechСredits { get; set; }

        public NUser(string userName, string password, int Credits, int TechСredits)
        {
            UserName = userName;
            Password = password;
            this.Credits = Credits;
            this.TechСredits = TechСredits;
        }
    }
}
