using RtsServer.App.Dto;

namespace RtsServer.App.DataBase.Dto
{
    public class UserAuth
    {
        public UserAuth(string userName, string password)
        {
            UserName = userName;
            Password = password;
        }

        public int Id { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public StatusUser Status;

    }
}
