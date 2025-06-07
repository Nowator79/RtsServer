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
        public int Level{ get; set; }
        public int Experience { get; set; }

        public NUser(string UserName, string Password, int Credits, int TechСredits, int Level, int Experience)
        {
            this.UserName = UserName;
            this.Password = Password;
            this.Credits = Credits;
            this.TechСredits = TechСredits;
            this.Level = Level;
            this.Experience = Experience;
        }

        public NUser(int Id, string UserName, int Level, int Experience)
        {
            this.Id = Id;
            this.UserName = UserName;
            this.Level = Level;
            this.Experience = Experience;
        }
    }
}
