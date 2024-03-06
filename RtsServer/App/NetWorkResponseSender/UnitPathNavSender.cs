using RtsServer.App.NetWork.Tcp;

namespace RtsServer.App.NetWorkResponseSender
{
    public class UnitPathNavSender : NetWorkSenderBase
    {
        public UnitPathNavSender(UserClientTcp clientApi) : base(clientApi)
        {
            response = new("battle", "/gameBattle/unitPathNav/", "200");
        }

        public override NetWorkSenderBase SetDate(object data)
        {
            response.SetBody(data);
            return this;
        }
    }
}
