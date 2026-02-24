using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RtsServer.App.Battle;
using RtsServer.App.Battle.Chat;
using RtsServer.App.DataBase.Db;
using RtsServer.App.DataBase.Dto;
using RtsServer.App.NetWork.Tcp;
using RtsServer.App.NetWorkHandlers;

namespace RtsServer.App
{
    public sealed class GameServer : IDisposable
    {
        private readonly ILogger<GameServer> _logger;
        private readonly CancellationTokenSource _cts = new();
        private bool _disposed;

        public int Port { get; }
        public Server TcpServer { get; private set; }
        public Router Router { get; }
        public DbUsers DbUsers { get; }
        public BattleManager BattleManager { get; }
        public ChatSystem ChatSystem { get; }
        public ConcurrentBag<Action> ActionsUpdate { get; } = new();

        public GameServer(int port, CancellationToken cancellationToken)
        {
            Port = port;

            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            _logger = loggerFactory.CreateLogger<GameServer>();


            Router = new Router();
            Router.SetContext(this);

            DbUsers = new DbUsers();
            BattleManager = new BattleManager(this, cancellationToken);
            ChatSystem = new ChatSystem(this);

            _logger.LogInformation("GameServer инициализирован на порту {Port}", port);
        }

        public async Task RunAsync()
        {
            var processor = new MainProcessor();
            processor.SetContext(this);

            TcpServer = new Server(Port, processor, _logger);
            TcpServer.Run();

            ActionsUpdate.Add(CheckPingClients);

            // Запускаем фоновый цикл обновления
            _ = Task.Run(UpdateLoopAsync, _cts.Token);

            _logger.LogInformation("Сервер запущен");
        }

        private async Task UpdateLoopAsync()
        {
            try
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    await Task.Delay(1000, _cts.Token);
                    await UpdateAsync();
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Цикл обновления остановлен");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Ошибка в цикле обновления");
            }
        }

        private async Task UpdateAsync()
        {
            try
            {
                foreach (var action in ActionsUpdate)
                {
                    action();
                }

                if (ConfigGameServer.IsEnabledClearConsole)
                {
                    Console.Clear();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обновлении");
            }
        }

        public List<UserAuth> GetAuthenticatedUsers()
        {
            return TcpServer.Users
                .Where(client => client.User != null)
                .Select(client => client.User!)
                .ToList();
        }

        /// <summary>
        /// Вызывается при отключении клиента (таймаут, закрытие сокета).
        /// Завершает матч для этого игрока, чтобы матч не «висел», если вышел единственный игрок.
        /// </summary>
        public void OnUserDisconnected(UserAuth? user)
        {
            if (user == null) return;
            BattleManager.EndBattleByUser(user);
        }

        private void CheckPingClients()
        {
            try
            {
                var disconnectedClients = TcpServer.Users
                    .Where(client => !client.IsConnected())
                    .ToList();

                foreach (var client in disconnectedClients)
                {
                    _logger.LogInformation("Клиент {ClientId} отключен по таймауту", client.Id);
                    client.Dispose();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при проверке пинга клиентов");
            }
        }

        public async Task StopAsync()
        {
            if (_disposed) return;

            _logger.LogInformation("Остановка сервера...");

            _cts.Cancel();
            TcpServer?.Dispose();

            if (ConfigGameServer.IsEnabledClearConsole)
            {
                Console.Clear();
            }

            _logger.LogInformation("Сервер остановлен");
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            StopAsync().GetAwaiter().GetResult();
            _cts.Dispose();

            GC.SuppressFinalize(this);
        }
    }
}