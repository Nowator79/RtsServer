using RtsServer.App.NetWork.Tcp;
using RtsServer.App.NetWorkDto.Response;
using RtsServer.App.NetWorkHandlers.Auth;
using RtsServer.App.NetWorkHandlers.Game;
using RtsServer.App.NetWorkHandlers.Game.Battle;
using RtsServer.App.NetWorkHandlers.Game.Chat;
using RtsServer.App.NetWorkHandlers.Main;
using System;
using System.Collections.Generic;
using System.Threading;

namespace RtsServer.App.NetWorkHandlers
{
    public class Router
    {
        private readonly Dictionary<string, IProcessor> processorsAll = new();
        public GameServer? GameServer { get; private set; }

        public Router()
        {
            RegisterDefaultHandlers();
        }

        public void AddProcessor(string action, IProcessor processor)
        {
            if (string.IsNullOrWhiteSpace(action) || processor == null)
                throw new ArgumentException("Некорректный action или processor");

            processorsAll[action] = processor;
        }

        public void Do(MainResponse mainResponse, UserClientTcp clientTcp, CancellationToken cancellationToken)
        {
            if (GameServer == null) throw new InvalidOperationException();
            if (mainResponse == null || string.IsNullOrEmpty(mainResponse.Action))
            {
                Console.WriteLine("[Router] Получен некорректный запрос");
                return;
            }

            if (processorsAll.TryGetValue(mainResponse.Action, out var processor))
            {
                processor.Handler(mainResponse, GameServer, clientTcp, cancellationToken);
            }
            else
            {
                Console.WriteLine($"[Router] Неизвестный экшен: {mainResponse.Action}");
                // Опционально: можно отправить ответ клиенту о неизвестном запросе
            }
        }

        public void SetContext(GameServer gameServer)
        {
            GameServer = gameServer ?? throw new ArgumentNullException(nameof(gameServer));
        }

        private void RegisterDefaultHandlers()
        {
            RegisterAuthHandlers();
            RegisterBattleHandlers();
            RegisterChatHandlers();
            RegisterMainHandlers();
        }


        private void RegisterAuthHandlers()
        {
            AddProcessor("/auth/regist/", new Regist());
            AddProcessor("/auth/login/", new Login());
            AddProcessor("/auth/ping/", new Ping());
        }
        private void RegisterMainHandlers()
        {
            AddProcessor("/main/askMapByCode/", new AskMapByCodeProcessor());
        }

        private void RegisterBattleHandlers()
        {
            AddProcessor("/gameBattle/addQueue/", new BattleAddQueue());
            AddProcessor("/gameBattle/cancelSearchBattle/", new BattleCancelQueue());
            AddProcessor("/gameBattle/battleIsReady/", new BattlePlayerIsReady());
            AddProcessor("/gameBattle/exitBattle/", new BattleExit());
            AddProcessor("/gameBattle/get/", new GetGameForUser());
            AddProcessor("/gameBattle/setChunk/", new SetChunkProcessor());
            AddProcessor("/gameBattle/unitSetTarget/", new SetTargetUnitsProcessor());
            AddProcessor("/gameBattle/unitSetTarget/attack/", new BuildConstructionProcessor());
        }

        private void RegisterChatHandlers()
        {
            AddProcessor("/chat/open/", new Open());
            AddProcessor("/chat/send/", new SendMessage());
        }
    }
}
