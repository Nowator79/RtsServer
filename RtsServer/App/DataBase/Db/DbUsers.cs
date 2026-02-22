
using RtsServer.App.DataBase.Dto;

namespace RtsServer.App.DataBase.Db
{
    public class DbUsers
    {
        private List<UserAuth> Users { get; set; }

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

        /// <summary>
        /// Пока не использовать, не работает как надо
        /// </summary>
        /// <returns></returns>
        public List<UserAuth> GetAll()
        {
            throw new Exception("Не использовать");
            return Users;
        }
    }
}
