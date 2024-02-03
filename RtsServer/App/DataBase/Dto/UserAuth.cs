using RtsServer.App.Dto;

namespace RtsServer.App.DataBase.Dto
{
    public class UserAuth
    {
        public UserAuth(string userName, string password)
        {
            UserName = userName;
            Password = password;
            Credits = 0;
            TechCredits = 0;
        }

        public UserAuth(string userName, string password, int Credits, int TechCredits)
        {
            UserName = userName;
            Password = password;
            this.Credits = Credits;
            this.TechCredits = TechCredits;
        }

        public int Id { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public int Credits { get; set; }
        public int TechCredits { get; set; }

        public StatusUser Status;
    }
}
