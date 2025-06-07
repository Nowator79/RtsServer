using RtsServer.App.NetWork.Tcp;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace RtsServer.App.NetWorkResponseSender
{
    public class StartGameSender : NetWorkSenderBase
    {
        public StartGameSender(UserClientTcp clientApi) : base(clientApi)
        {
            _response = new("battle", "/gameBattle/startGame/", "", "200");
        }

        public override NetWorkSenderBase SetDate(object data)
        {
            _response.SetBody(data);
            return this;
        }

        public async Task SendAsync(CancellationToken cancellationToken = default)
        {
            await _clientApi.WriteAsync(_response, cancellationToken);
        }
    }
}