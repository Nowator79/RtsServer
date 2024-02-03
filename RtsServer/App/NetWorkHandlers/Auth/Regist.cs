using RtsServer.App.Adapters;
using RtsServer.App.DataBase;
using RtsServer.App.DataBase.Dto;
using RtsServer.App.NetWork.Tcp;
using RtsServer.App.NetWorkDto;
using RtsServer.App.NetWorkDto.Response;

namespace RtsServer.App.NetWorkHandlers.Auth
{
    public class Regist : IProcessor
    {
        public void Handler(MainResponse response, GameServer context, UserClientTcp clientTcp)
        {
            NUser? userAuth = response.GetBody<NUser>();
            if (userAuth != null)
            {
                using ApplicationContext db = new();
                UserAuth? userFind = db.Users.FirstOrDefault(e => e.UserName == userAuth.Value.UserName);

                if (userFind != null) return;

                db.Users.AddRange(UserAdapter.Get(userAuth.Value));
                db.SaveChanges();
            }
        }
    }
}
