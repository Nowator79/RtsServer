using RtsServer.App.DataBase.Dto;
using RtsServer.App.NetWorkHandlers;
using RtsServer.App.NetWorkResponseSender;
using System.Net;
using System.Net.Sockets;

/*
 Класс описывает работу сервера TCP
 */
namespace RtsServer.App.NetWork.Tcp
{
    public abstract class Base : INetWorkServer
    {
        protected TcpListener tcpListener;
        protected int port = 9080;
        protected MainProcessor processor;
        protected List<UserClientTcp> users;
        protected CancellationTokenSource cancellationTokenSource;

        public List<UserClientTcp> GetUsers()
        {
            return users;
        }

        public Base(int port, MainProcessor processor)
        {
            this.port = port;
            this.processor = processor;

            tcpListener = new(IPAddress.Any, port);

            users = new();
            cancellationTokenSource = new CancellationTokenSource();
        }

        private void ProcessClient(TcpClient tcpClient)
        {
            UserClientTcp client = new(tcpClient, this);
            Thread clientListen = new(client.Listen)
            {
                Name = "Listener: user" + client.Id
            };
            clientListen.Start();
            users.Add(client);
        }

        public UserClientTcp? GetClientById(string id) => users.Find(n => n.Id == id);
        public UserClientTcp? GetClientByUserAuth(UserAuth user) => users.Find(n => n.User == user);
        public void Run()
        {
            cancellationTokenSource.Token.Register(() => tcpListener.Stop());
            tcpListener.Start();
            Task.Run(async () =>
            {
                try
                {
                    Console.WriteLine("Сервер запущен. Ожидание подключений... ");

                    while (true)
                    {
                        TcpClient tcpClient = await Task.Run(
                            tcpListener.AcceptTcpClientAsync,
                            cancellationTokenSource.Token);

                        ProcessClient(tcpClient);

                    }
                }
                finally
                {
                    tcpListener.Stop();
                    Console.WriteLine("Tcp Server is stoped");
                }
            });
           
        }
        public void DisconectUser(UserClientTcp userClient)
        {

            users.Remove(userClient);
        }
        public void Exit()
        {
            tcpListener.Stop();
            cancellationTokenSource.Cancel();
        }
        public MainProcessor GetProcessor() => processor;
    }
}
