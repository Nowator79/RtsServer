﻿using RtsServer.App.DataBase.Dto;
using RtsServer.App.NetWorkDto;
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
            byte[] r = Array.Empty<byte>();
            List<byte> response = new();
            MainResponse? mainResponse;
            CountRead++;
            int bytesRead;
            while (true)
            {
                if (Stream.CanRead)
                {
                    while ((bytesRead = Stream.ReadByte()) != '\n')
                    {
                        r = r.Append((byte)bytesRead).ToArray();
                        response.Add((byte)bytesRead);
                    }
                    string word = Encoding.UTF8.GetString(response.ToArray());
                    Stream.Flush();

                    mainResponse = JsonSerializer.Deserialize<MainResponse>(word);
                    //return new MainResponse("getMap", "/gameBattle/get/", "200");
                    if (mainResponse == null) throw new Exception("Ответ не в JSON");
                    return mainResponse;
                }
                Thread.Sleep(100);
            }

        }
        
        public async void Write(MainResponse response)
        {
            string request = JsonSerializer.Serialize(response);
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
            Console.WriteLine(timeDef);
            return timeDef > -5;
        }
    }
}