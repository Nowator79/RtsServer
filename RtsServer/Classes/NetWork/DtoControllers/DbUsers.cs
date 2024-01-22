using RtsServer.Classes.NetWork.Dto;

namespace RtsServer.Classes.NetWork.DtoControllers
{
    public class DbUsers
    {
        private List<UserAuth> Users;

        public DbUsers()
        {
            Users = new();
        }
        public void Add(UserAuth user)
        {
            UserAuth? findUser = Users.Find(e => e.UserName == user.UserName);
            if(findUser == null)
            {
                Users.Add(user);
            }
        }
        public UserAuth? GetByLogin(string login)
        {
            UserAuth? user = Users.Find(e => e.UserName == login);
            return user;
        }
        public List<UserAuth> GetAll()
        {
            return Users;
        }
    }
}
