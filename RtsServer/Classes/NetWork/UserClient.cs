using RtsServer.Classes.NetWork.Dto;
using RtsServer.Classes.Processor.Response;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace RtsServer.Classes.NetWork.Tcp
{
    public class UserClientTcp
    {
        public string Id { get; }

        private readonly NetworkStream Stream;
        private readonly INetWorkServer server;

        public int CountWrite { get; private set; } = 0;

        public int CountRead { get; private set; } = 0;

        public UserAuth User { get; private set; }

        public void SetUser(UserAuth User)
        {
            this.User = User;
        }
        public UserClientTcp(TcpClient tcpClient, INetWorkServer netWorkServer)
        {
            Id = "";

            Stream = tcpClient.GetStream();
            System.Net.EndPoint? remoteEndPoint = tcpClient.Client.RemoteEndPoint;
            if (remoteEndPoint == null) return;

            string? newId = remoteEndPoint.ToString();

            if (newId == null) return;

            Id = newId;
            server = netWorkServer;

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
            catch
            {
                Disconect();
            }
        }

        public void Disconect()
        {
            Console.WriteLine($"Клиент {Id} отключился");
            server.DisconectUser(this);
        }

        public MainResponse Read()
        {
            CountRead++;
            List<byte> response = new();
            int bytesRead;

            while ((bytesRead = Stream.ReadByte()) != '\n')
            {
                response.Add((byte)bytesRead);
            }
            string word = Encoding.UTF8.GetString(response.ToArray());


            MainResponse? mainResponse = JsonSerializer.Deserialize<MainResponse>(word);
            if (mainResponse == null) throw new Exception("Ответ не в JSON");

            return mainResponse;
        }

        public async void Write(MainResponse response)
        {
            string request = JsonSerializer.Serialize(response);
            request += '\n';
            await Stream.WriteAsync(Encoding.UTF8.GetBytes(request));
            CountWrite++;
        }
    }
}
