using RtsServer.App.NetWork.Tcp;
using System.Text.Json;

namespace RtsServer.App.NetWorkResponseSender
{
    public class UnitInfoSender : NetWorkSenderBase
    {
        public UnitInfoSender(UserClientTcp clientApi) : base(clientApi)
        {
            _response = new("gameBattle", "/gameBattle/unit/info/", "", "200");
        }

        public override NetWorkSenderBase SetDate(object data)
        {
            _response.SetBody(data);
            return this;
        }
    }
}
