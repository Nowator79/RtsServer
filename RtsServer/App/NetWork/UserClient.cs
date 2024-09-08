using RtsServer.App.DataBase.Dto;
using RtsServer.App.NetWorkDto.Response;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace RtsServer.App.NetWork.Tcp
{
    public class UserClientTcp
    {
        public string Id { get; }

        public readonly TcpClient TcpClient;
        public readonly NetworkStream Stream;
        private readonly INetWorkServer server;

        public long LastPing { get; private set; }

        public int CountWrite { get; private set; } = 0;

        public int CountRead { get; private set; } = 0;

        public UserAuth User { get; private set; }
        protected bool _isDisconnect { get; set; } = false;

        public void SetUser(UserAuth User)
        {
            this.User = User;
        }
        public UserClientTcp(TcpClient tcpClient, INetWorkServer netWorkServer)
        {
            Id = "";
            TcpClient = tcpClient;
            Stream = tcpClient.GetStream();
            System.Net.EndPoint? remoteEndPoint = tcpClient.Client.RemoteEndPoint;
            if (remoteEndPoint == null) return;

            string? newId = remoteEndPoint.ToString();

            if (newId == null) return;

            Id = newId;
            server = netWorkServer;

            UpdatePing();
        }

        public void Listen()
        {
            try
            {
                while (true)
                {
                    MainResponse mainResponse = Read();
                    server.GetProcessor().Handler(mainResponse, this);  // event write client
                }
            }
            catch(Exception ex) 
            {
                Console.WriteLine(ex.Message);
                Disconnect();
            }
        }

        public void Disconnect()
        {
            Console.WriteLine($"Клиент {Id} отключился");
            _isDisconnect = true;
            server.DisconectUser(this);
        }


        public MainResponse Read()
        {
            byte[] r = Array.Empty<byte>();
            List<byte> response = new();
            MainResponse? mainResponse;
            CountRead++;
            int bytesRead;

            while (!_isDisconnect)
            {
                if (Stream.CanRead && !_isDisconnect)
                {
                    while ((bytesRead = Stream.ReadByte()) != '\n' && !_isDisconnect)
                    {
                        r = r.Append((byte)bytesRead).ToArray();
                        response.Add((byte)bytesRead);
                    }
                    if (_isDisconnect)
                    {
                        throw new Exception("Выход из чтения");
                    }
                    string word = Encoding.UTF8.GetString(response.ToArray());
                    Stream.Flush();

                    mainResponse = JsonSerializer.Deserialize<MainResponse>(word);

                    if (ConfigGameServer.IsDebugNetWork)
                    {
                        Console.WriteLine($"Get {word}");
                    }

                    //return new MainResponse("getMap", "/gameBattle/get/", "200");
                    if (mainResponse == null) throw new Exception("Ответ не в JSON");
                    return mainResponse;
                }
                Thread.Sleep(100);
            }
            throw new Exception("Выход из чтения");
        }

        public async void Write(MainResponse response)
        {
            string request = JsonSerializer.Serialize(response);
            if (ConfigGameServer.IsDebugNetWork)
            {
                Console.WriteLine($"Send {request}");
            }
            request += '\n';
            await Stream.WriteAsync(Encoding.UTF8.GetBytes(request));
            CountWrite++;
        }

        public void UpdatePing()
        {
            LastPing = DateTime.Now.Ticks;
        }

        public bool IsConnectPing()
        {
            double timeDef = (new TimeSpan(LastPing)).TotalSeconds - (new TimeSpan(DateTime.Now.Ticks)).TotalSeconds;
            return timeDef > -5;
        }

    }
}
