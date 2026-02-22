using RtsServer.App.NetWork.Tcp;

namespace RtsServer.App.NetWorkResponseSender
{
    public class LoadMapSender : NetWorkSenderBase
    {
        public LoadMapSender(UserClientTcp clientApi) : base(clientApi)
        {
            _response = new("main", "/main/loadMap/", "", "200");
        }
        public override NetWorkSenderBase SetDate(object data)
        {
            _response.SetBody(data);
            return this;
        }
    }
}
