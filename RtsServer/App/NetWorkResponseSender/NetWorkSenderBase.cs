using RtsServer.App.NetWork.Tcp;
using RtsServer.App.NetWorkDto.Response;

namespace RtsServer.App.NetWorkResponseSender
{
    public class NetWorkSenderBase
    {
        protected UserClientTcp clientApi;
        protected MainResponse response;

        public NetWorkSenderBase(UserClientTcp clientApi)
        {
            this.clientApi = clientApi;
            response = new("base", "/base/", "200");
        }

        public virtual NetWorkSenderBase SetDate(IResponse data)
        {
            response.SetBody(data);
            return this;
        }

        public void SendMessage()
        {
            clientApi.Write(response);
        }
    }
}
