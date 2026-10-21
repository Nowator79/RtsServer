using System.Security.Cryptography;
using RtsServer.App.DataBase.Dto;

namespace RtsServer.App.DataBase
{
    public static class AuthService
    {
        public static string IssueToken()
        {
            Span<byte> bytes = stackalloc byte[24];
            RandomNumberGenerator.Fill(bytes);
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        public static UserAuth? Login(ApplicationContext db, string? userName, string? password, string? token)
        {
            if (!string.IsNullOrWhiteSpace(token))
            {
                return db.Users.FirstOrDefault(u => u.AuthToken == token);
            }

            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
                return null;

            return db.Users.FirstOrDefault(u => u.UserName == userName && u.Password == password);
        }

        public static void RefreshToken(ApplicationContext db, UserAuth user)
        {
            user.AuthToken = IssueToken();
            db.SaveChanges();
        }
    }
}
