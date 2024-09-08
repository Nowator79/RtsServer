using RtsServer.App.DataBase.Dto;

namespace RtsServer.App.Battle.Chat
{
    public class User
    {
        public UserAuth UserAuth { get; set; }

        public User(UserAuth userAuth)
        {
            UserAuth = userAuth;
        }
    }

}
