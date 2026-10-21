
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
            if (findUser == null)
            {
                Users.Add(user);
                return;
            }

            findUser.AuthToken = user.AuthToken;
            findUser.Credits = user.Credits;
            findUser.TechCredits = user.TechCredits;
            findUser.Experience = user.Experience;
            findUser.Level = user.Level;
        }

        public UserAuth? GetByToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return null;
            return Users.Find(e => e.AuthToken == token);
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
