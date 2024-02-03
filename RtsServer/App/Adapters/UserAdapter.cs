using RtsServer.App.DataBase.Dto;
using RtsServer.App.NetWorkDto;

namespace RtsServer.App.Adapters
{
    public static class UserAdapter
    {
        public static NUser Get(UserAuth user)
        {
            return new(user.UserName, user.Password, user.Credits, user.TechCredits);
        }
        public static UserAuth Get(NUser user)
        {
            return new(user.UserName, user.Password) {Credits = user.Credits, TechCredits = user.TechСredits };
        }
    }
}
