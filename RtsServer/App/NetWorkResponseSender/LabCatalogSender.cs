using RtsServer.App.NetWork.Tcp;

namespace RtsServer.App.NetWorkResponseSender
{
    public class LabCatalogSender : NetWorkSenderBase
    {
        public LabCatalogSender(UserClientTcp clientApi) : base(clientApi)
        {
            _response = new("lab", "/lab/catalog/", "", "200");
        }

        public override NetWorkSenderBase SetDate(object data)
        {
            _response.SetBody(data);
            return this;
        }
    }
}
