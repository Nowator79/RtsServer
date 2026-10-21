using RtsServer.App.DataBase;
using RtsServer.App.DataBase.Dto;
using RtsServer.App.NetWorkDto;

namespace RtsServer.App.Adapters
{
    public static class UserAdapter
    {
        /// <summary>Полный профиль себе: с токеном, без пароля.</summary>
        public static NUser Get(UserAuth user)
        {
            return new NUser(
                user.Id,
                user.UserName,
                user.AuthToken ?? "",
                user.Credits,
                user.TechCredits,
                PlayerProgression.NormalizeLevel(user.Level),
                Math.Max(0, user.Experience));
        }

        /// <summary>Публичный профиль в чат/списки — без токена и пароля.</summary>
        public static NUser GetPublic(UserAuth user)
        {
            return new NUser(
                user.Id,
                user.UserName,
                "",
                user.Credits,
                user.TechCredits,
                PlayerProgression.NormalizeLevel(user.Level),
                Math.Max(0, user.Experience));
        }

        public static UserAuth Get(NUser user)
        {
            return new UserAuth(user.UserName, user.Password)
            {
                Credits = user.Credits,
                TechCredits = user.TechCredits,
                Level = user.Level,
                Experience = user.Experience,
            };
        }
    }
}
