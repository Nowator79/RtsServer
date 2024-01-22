using RtsServer.Classes.Processor;
using System.Net;
using System.Net.Sockets;

/*
 Класс описывает работу сервера TCP
 */
namespace RtsServer.Classes.NetWork.Tcp
{
    public abstract class Base : INetWorkServer
    {
        protected TcpListener tcpListener;
        protected bool isStope = false;
        protected int port = 9080;
        protected MainProcessor processor;
        protected List<UserClientTcp> users;

        public List<UserClientTcp> GetUsers()
        {
            return users;
        }

        public Base(int port, MainProcessor processor)
        {
            this.port = port;
            this.processor = processor;

            tcpListener = new (IPAddress.Any, port);

            users = new();
        }

        public void Close()
        {
            isStope = true;
            tcpListener.Stop();
        }

        private void ProcessClient(TcpClient tcpClient)
        {
            UserClientTcp client = new(tcpClient, this);
            Task.Run(() => { client.Listen(); });
            Console.WriteLine($"Клиент {client.Id} подключился");
            users.Add(client);
        }

        public UserClientTcp? GetClientById(string id)
        {
            return users.Find(n => n.Id == id);
        }

        public async void Run()
        {
            try
            {
                tcpListener.Start();
                Console.WriteLine("Сервер запущен. Ожидание подключений... ");

                while (true)
                {
                    TcpClient tcpClient = await tcpListener.AcceptTcpClientAsync();

                    ProcessClient(tcpClient);

                    if (isStope) break;

                }
            }
            finally
            {
                tcpListener.Stop();
            }
        }

        public void DisconectUser(UserClientTcp userClient)
        {
            users.Remove(userClient);
        }

        public void Exit()
        {
            throw new NotImplementedException();
        }

        public MainProcessor GetProcessor()
        {
            return processor;
        }
    }
}
