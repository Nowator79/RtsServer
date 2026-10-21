using System.ComponentModel.DataAnnotations.Schema;
using RtsServer.App.Dto;

namespace RtsServer.App.DataBase.Dto
{
    public class UserAuth
    {
        public UserAuth()
        {
            UserName = "";
            Password = "";
            AuthToken = "";
        }

        public UserAuth(string userName, string password)
        {
            UserName = userName;
            Password = password;
            AuthToken = "";
            Credits = 0;
            TechCredits = 0;
            Experience = 0;
            Level = 0;
        }

        public UserAuth(string userName, string password, int credits, int techCredits, int experience, int level)
        {
            UserName = userName;
            Password = password;
            AuthToken = "";
            Credits = credits;
            TechCredits = techCredits;
            Experience = experience;
            Level = level;
        }

        public int Id { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        /// <summary>Токен сессии. Клиент сохраняет и шлёт при входе вместо пароля.</summary>
        public string AuthToken { get; set; }
        public int Credits { get; set; }
        public int TechCredits { get; set; }
        public int Experience { get; set; }
        public int Level { get; set; }

        [NotMapped]
        public StatusUser Status;
    }
}
