using RtsServer.App.DataBase.Dto;

namespace RtsServer.App.Battle.Chat
{
    public class ChatSystem
    {
        public List<User> Users { get; set; }


        public ChatSystem()
        {
            Users = [];
        }

        public void AddUser(User user)
        {
            Users.Add(user);
        }

        public void RemoveUser(User user)
        {
            Users.Remove(user);
        }

        public User FindUserByUserAuth(UserAuth userAuth)
        {
           return Users.First(user => user.UserAuth == userAuth);
        }
    }
}
