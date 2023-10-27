using RtsServer.Classes.DataBase;
using RtsServer.Classes.NetWork.Dto;
using RtsServer.Classes.NetWork.Tcp;
using RtsServer.Classes.Processor.Response;

namespace RtsServer.Classes.Processor.Auth
{
    public class Regist : ProcessorBase
    {
        public override void Handler(MainResponse response, GameServer context, UserClientTcp clientTcp)
        {
            UserAuth? userAuth = response.GetBody<UserAuth>();
            if (userAuth != null)
            {
                using (ApplicationContext db = new())
                {
                    UserAuth? userFind = db.Users.FirstOrDefault(e => e.UserName == userAuth.UserName);

                    if (userFind != null) return;

                    db.Users.AddRange(userAuth);
                    db.SaveChanges();
                }
            }
        }
    }
}
