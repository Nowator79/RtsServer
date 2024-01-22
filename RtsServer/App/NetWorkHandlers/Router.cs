using RtsServer.App.NetWork.Tcp;
using RtsServer.App.NetWorkDto.Response;
using RtsServer.App.NetWorkHandlers.Auth;
using RtsServer.App.NetWorkHandlers.Game;
using RtsServer.App.NetWorkHandlers.Game.Battle;

namespace RtsServer.App.NetWorkHandlers
{
    public class Router
    {
        public Dictionary<string, IProcessor> processorsAll { get; private set; }
        public GameServer gameServer { get; private set; }
        public Router()
        {
            processorsAll = new Dictionary<string, IProcessor>
            {
                {"/auth/regist/", new Regist() },
                {"/auth/login/", new Login() },
                {"/auth/ping/", new Ping() },
                {"/gameBattle/addQueue/", new BattleAddQueue() },
                {"/gameBattle/get/", new GetGameForUser() },
                {"/gameBattle/setChunk/", new SetChunkProcessor() },
                {"/gameBattle/unitSetTarget/", new SetTargetUnitsProcessor() },
            };
        }
        public void AddProcessor(string action, IProcessor processor)
        {
            processorsAll.Add(action, processor);
        }
        public void Do(MainResponse mainResponse, UserClientTcp clientTcp)
        {
            if (processorsAll[mainResponse.action] != null)
            {
                processorsAll[mainResponse.action].Handler(mainResponse, gameServer, clientTcp);
            }
        }
        public void SetContext(GameServer gameServer)
        {
            this.gameServer = gameServer;
        }
    }
}
