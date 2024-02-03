using RtsServer.App.Buttle;
using RtsServer.App.DataBase.Db;
using RtsServer.App.NetWork.Tcp;
using RtsServer.App.NetWorkHandlers;

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
        private bool IsCancel { get; set; } = false;
        public GameServer(int port)
        {
            this.port = port;

            Router = new();
            Router.SetContext(this);

            DbUsers = new();
            ButtleManager = new(this);
            ActionsUpdate = new();

        }

        public void Run()
        {
            MainProcessor processor = new();
            processor.SetContext(this);

            TcpServer = new(port, processor);
            TcpServer.Run();


            ActionsUpdate.Add(CheckPing);
            //ActionsUpdate.Add(() =>
            //{
            //    List<UserClientTcp> userClients = TcpServer.GetUsers();
            //    UserViewer.View(userClients);
            //});

            //ActionsUpdate.Add(() =>
            //{
            //    ButtleManager.Games.ForEach(game =>
            //    {
            //        GameViewer.ViewFullInfo(game);
            //    });
            //});

            Thread thread = new(Loop);
            thread.Start();
            thread.Name = "GameServer";
        }

        private void Loop()
        {
            while (!IsCancel)
            {
                Thread.Sleep(1000);
                Update();
            }

             if(ConfigGameServer.IsEnabledClearConsole) Console.Clear();
            Console.WriteLine("Server stop");
        }

        private void Update()
        {
            foreach (Action action in ActionsUpdate)
            {
                action();
            }
        }

        private void CheckPing()
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

        public void Cancel()
        {
            IsCancel = true;
            TcpServer.Exit();
        }
    }
}
