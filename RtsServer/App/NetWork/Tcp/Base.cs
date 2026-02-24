using Microsoft.Extensions.Logging;
using RtsServer.App.DataBase.Dto;
using RtsServer.App.NetWorkHandlers;
using RtsServer.App.NetWorkResponseSender;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

/*
 Класс описывает работу сервера TCP
 */

namespace RtsServer.App.NetWork.Tcp
{
    public abstract class Base : INetWorkServer, IDisposable
    {
        private readonly TcpListener _tcpListener;
        private readonly ConcurrentDictionary<string, UserClientTcp> _users = new();
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private readonly ILogger<GameServer> _logger;
        protected readonly MainProcessor _processor;
        private bool _disposed;

        public IEnumerable<UserClientTcp> Users => _users.Values;

        public Base(int port, MainProcessor processor, ILogger<GameServer> logger)
        {
            _processor = processor;
            _logger = logger;
            _tcpListener = new TcpListener(IPAddress.Any, port);
        }

        public UserClientTcp? GetClientById(string id) =>
            _users.TryGetValue(id, out var client) ? client : null;

        public UserClientTcp? GetClientByUserAuth(UserAuth user) =>
            _users.Values.FirstOrDefault(n => n.User == user);

        public void Run()
        {
            _tcpListener.Start();
            _logger.LogInformation("Сервер запущен на порту {Port}. Ожидание подключений...", ((IPEndPoint)_tcpListener.LocalEndpoint).Port);

            Task.Run(async () =>
            {
                try
                {
                    while (!_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        var tcpClient = await _tcpListener.AcceptTcpClientAsync(_cancellationTokenSource.Token);
                        _ = ProcessClientAsync(tcpClient);
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Работа сервера остановлена по запросу.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Критическая ошибка в работе сервера");
                }
            }, _cancellationTokenSource.Token);
        }

        private async Task ProcessClientAsync(TcpClient tcpClient)
        {
            try
            {
                var client = new UserClientTcp(tcpClient, this);
                _users.TryAdd(client.Id, client);
                _logger.LogInformation("Клиент {ClientId} подключен.", client.Id);
                await client.ListenAsync(_cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка обработки клиента");
                tcpClient.Dispose();
            }
            finally
            {
                _logger.LogInformation("Клиент отключен.");
            }
        }

        public void DisconnectUser(UserClientTcp userClient)
        {
            if (userClient == null) return;

            var user = userClient.User;
            _users.TryRemove(userClient.Id, out _);
            _logger.LogInformation("Клиент {ClientId} отключен.", userClient.Id);

            // Завершаем матч для отключившегося игрока (чтобы матч не жил, если вышел единственный игрок)
            _processor.GameServer?.OnUserDisconnected(user);
        }

        public void Exit()
        {
            Dispose();
        }

        public MainProcessor GetProcessor() => _processor;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _logger.LogInformation("Завершение работы сервера...");

            _cancellationTokenSource.Cancel();
            _tcpListener.Stop();

            foreach (var user in _users.Values)
            {
                user.Dispose();
            }

            _cancellationTokenSource.Dispose();
            _logger.LogInformation("Сервер остановлен.");
        }
    }
}