using RtsServer.App.NetWork.Tcp;

namespace RtsServer.App.NetWorkResponseSender
{
    public class PlayerStateSender : NetWorkSenderBase
    {
        public PlayerStateSender(UserClientTcp clientApi) : base(clientApi)
        {
            _response = new("battle", "/gameBattle/player/state/", "", "200");
        }

        public override NetWorkSenderBase SetDate(object data)
        {
            _response.SetBody(data);
            return this;
        }
    }
}
