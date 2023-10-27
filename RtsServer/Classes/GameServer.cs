using RtsServer.Classes.Buttle;
using RtsServer.Classes.DataBase;
using RtsServer.Classes.NetWork.Dto;
using RtsServer.Classes.NetWork.DtoControllers;
using RtsServer.Classes.NetWork.Tcp;
using RtsServer.Classes.Processor;
using RtsServer.Classes.Processor.Response;
using System.Text;
using System.Text.Json;

namespace RtsServer.Classes
{
    public class GameServer
    {
        private readonly int port = 0;
        public Server TcpServer { get; private set; }
        public Router Router { get; private set; }
        public DbUsers DbUsers { get; private set; }
        public ButtleManager ButtleManager { get; private set; }


        public GameServer(int port)
        {
            this.port = port;

            Router = new();
            DbUsers = new();

            Router.SetContext(this);

            ButtleManager = new();
        }


        public void Run()
        {
            MainProcessor processor = new();
            processor.SetContext(this);

            TcpServer = new(port, processor);
            TcpServer.Run();

            Task.Run(() => { Loop(); });

            Game game = new();
            ButtleManager.Add(game);
            game.GenerateRandomMap();
            game.Start();
        }

        public void Loop()
        {
            while (true)
            {
                Update();
                Thread.Sleep(2000);

                TcpServer.GetUsers().ForEach(user =>
                {
                    //MainResponse response = new("ping", "ping", "200");
                    //user.Write(response);
                });

            }
        }

        /// <summary>
        /// Метод обновление состояний сервера
        /// </summary>
        public void Update()
        {
            //ViewConsole();
        }

        public void ViewConsole()
        {
            Console.Clear();
            {
                List<UserClientTcp> userClients = TcpServer.GetUsers();
                Console.WriteLine($"{"ID",20} | {"CountRead",10} |  {"CountWrite",10}");
                foreach (UserClientTcp userClient in userClients)
                {
                    Console.Write($"{userClient.Id,20} | {userClient.CountRead,10} |  {userClient.CountWrite,10} |");
                    if (userClient.User != null)
                    {
                        Console.Write($"{userClient.User.Id,10} | {userClient.User.UserName,10}");
                    }
                    Console.WriteLine(" ");
                    Console.WriteLine(new StringBuilder().Insert(0, "-", 70).ToString());
                }
            }
            Console.WriteLine(new StringBuilder().Insert(0, "=", 60).ToString());
            /*
            using (ApplicationContext db = new())
            {
                var users = db.Users.ToList();
                Console.WriteLine($"{"login",20} | {"password",10}");
                foreach (UserAuth user in users)
                {
                    Console.WriteLine($"{user.UserName,20} | {user.Password,10}");
                    Console.WriteLine(new StringBuilder().Insert(0, "-", 40).ToString());
                }
            }
            */
        }
    }
}
