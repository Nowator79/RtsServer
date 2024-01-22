using RtsServer.App.Buttle;
using RtsServer.App.Buttle.MapButlle;
using RtsServer.App.DataBase.Db;
using RtsServer.App.NetWork.Tcp;
using RtsServer.App.NetWorkHandlers;
using RtsServer.App.ViewConsole;

namespace RtsServer.App
{
    public class GameServer
    {
        private readonly int port = 0;
        public Server TcpServer { get; private set; }
        public Router Router { get; private set; }
        public DbUsers DbUsers { get; private set; }
        public ButtleManager ButtleManager { get; private set; }
        public List<Action> ActionsUpdate { get; }

        public GameServer(int port)
        {
            this.port = port;

            Router = new();
            DbUsers = new();
            ButtleManager = new(this);
            ActionsUpdate = new();

            Router.SetContext(this);
        }


        public void Run()
        {
            MainProcessor processor = new();
            processor.SetContext(this);

            TcpServer = new(port, processor);
            TcpServer.Run();


            ActionsUpdate.Add(checkPing);
            ActionsUpdate.Add(() =>
            {
                Console.Clear();
                List<UserClientTcp> userClients = TcpServer.GetUsers();
                UserViewer.View(userClients);
            });

            ActionsUpdate.Add(() =>
            {
                ButtleManager.Games.ForEach(game =>
                {
                    GameViewer.ViewFullInfo(game);
                    //MapViewer.View(game.Map);
                });
            });


            Task.Run(Loop);
        }

        private void Loop()
        {
            while (true)
            {
                Thread.Sleep(1000);

                Update();

                TcpServer.GetUsers().ForEach(user =>
                {
                    //MainResponse response = new("ping", "ping", "200");
                    //user.Write(response);
                });

            }
        }

        private void Update()
        {
            foreach (var action in ActionsUpdate)
            {
                action();
            }
        }

        private void checkPing()
        {
            List<UserClientTcp> clients = TcpServer.GetUsers();

            List<UserClientTcp> disconectsList = new();
            foreach (UserClientTcp client in clients)
            {
                if (!client.IsConnectPing())
                {
                    disconectsList.Add(client);
                }
            }
            foreach (UserClientTcp item in disconectsList)
            {
                clients.Remove(item);
                item.Disconect();

            }
            disconectsList.Clear();
        }
    }
}
