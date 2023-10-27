using RtsServer.Classes.NetWork.Tcp;
using RtsServer.Classes.Processor.Auth;
using RtsServer.Classes.Processor.Game;
using RtsServer.Classes.Processor.Response;
using System;

namespace RtsServer.Classes.Processor
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
                {"/gameBattle/get/", new GetGameForUser() },
                {"/gameBattle/setChunk/", new SetChunkProcessor() }
            };
        }
        public void AddProcessor(string action, IProcessor processor)
        {
            processorsAll.Add(action, processor);
        }
        public void Do(MainResponse mainResponse, UserClientTcp clientTcp)
        {
            Console.WriteLine(mainResponse.action);
            processorsAll[mainResponse.action].Handler(mainResponse, gameServer, clientTcp);
        }
        public void SetContext(GameServer gameServer)
        {
            this.gameServer = gameServer;
        }
    }
}
