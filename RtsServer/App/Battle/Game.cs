using Microsoft.Extensions.Logging;
using RtsServer.App.Adapters;
using RtsServer.App.Battle.Chat;
using RtsServer.App.Battle.Constructions;
using RtsServer.App.Battle.Dto;
using RtsServer.App.Battle.MapBattle;
using RtsServer.App.Battle.Navigator;
using RtsServer.App.Battle.Units;
using RtsServer.App.NetWork.Tcp;
using RtsServer.App.NetWorkDto.Response;
using RtsServer.App.NetWorkResponseSender;
using RtsServer.App.Tools;
using RtsServer.App.ViewConsole;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace RtsServer.App.Battle
{
    /**
     * Класс для работы игровой сессии
     */
    public sealed class Game : IDisposable
    {
        private readonly ILogger<Game> _logger;
        private readonly GroundUnitNavigator _navigator;
        private readonly CancellationTokenSource _cts = new();
        private bool _disposed;
        private int _unitNextId = 0;
        private int _constructionNextId = 0;
        bool _isPlaying = false;

        public int Id { get; }
        public long CreateDateTime { get; }
        public Map Map { get; private set; }
        public List<Player> Players { get; } = new();
        public List<Unit> Units { get; } = new();
        public List<Construction> Constructions { get; } = new();
        public BattleManager BattleManager { get; }
        public TimeSystem TimeSystem { get; }
        public event Action UpdateEvent;
        private CancellationToken _cancellationToken;

        public Game(int id, BattleManager battleManager, CancellationToken cancellationToken)
        {
            Id = id;
            BattleManager = battleManager;
            _cancellationToken = cancellationToken;

            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });
            _logger = loggerFactory.CreateLogger<Game>();

            CreateDateTime = DateTime.UtcNow.Ticks;
            TimeSystem = new TimeSystem();
            _navigator = new GroundUnitNavigator();

            _logger.LogInformation("Game {GameId} created", id);
        }

        public Game SetMap(Map map)
        {
            Map = map ?? throw new ArgumentNullException(nameof(map));
            _navigator.SetMap(map);
            return this;
        }

        public async Task StartAsync()
        {
            if (Map == null)
                throw new InvalidOperationException("Map not set");

            _logger.LogInformation("Starting game {GameId}", Id);

            _isPlaying = true;
            SetStatusUsersInGame();

            foreach (var player in Players)
            {
                var userTcp = BattleManager.GameServer.TcpServer.GetClientByUserAuth(player.UserAuth);
                if (userTcp != null)
                {
                    await new StartGameSender(userTcp)
                        .SetDate(new SetStartGameData(Map.Code, new Vector2Int()))
                        .SendAsync();
                }
            }

            SendUpdateGameAsync();
            UpdateEvent += SendUpdateGameAsync;
            UpdateEvent += TimeSystem.Update;

            _ = Task.Run(GameLoopAsync, _cts.Token);
        }

        private async void SendUpdateGameAsync()
        {
            var tasks = new List<Task>();
            var nGame = GameAdapter.Get(this);

            foreach (var player in Players)
            {
                var userTcp = BattleManager.GameServer.TcpServer.GetClientByUserAuth(player.UserAuth);
                if (userTcp != null)
                {
                    MainResponse response = new MainResponse("battle", "/gameBattle/setGame/", "", "200")
                        .SetBody(nGame);

                    tasks.Add(userTcp.WriteAsync(response, _cancellationToken));
                }
            }

            await Task.WhenAll(tasks);
        }

        private void SetStatusUsersInGame()
        {
            foreach (var player in Players)
            {
                player.UserAuth.Status.SetInGame();
            }
        }

        public void AddUnit(Unit unit)
        {
            unit.SetId(_unitNextId++);
            Units.Add(unit);
            _logger.LogDebug("Unit {UnitId} added to game {GameId}", unit.Id, Id);
        }

        public void AddConstruction(Construction construction)
        {
            construction.SetId(_constructionNextId++);
            Constructions.Add(construction);
            _logger.LogDebug("Construction {ConstructionId} added to game {GameId}", construction.Id, Id);
        }

        private async Task GameLoopAsync()
        {
            try
            {
                while (_isPlaying && !_cts.Token.IsCancellationRequested)
                {
                    await UpdateAsync();
                    await Task.Delay(50, _cts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Game loop stopped");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in game loop");
            }
        }

        public async Task UpdateAsync()
        {
            UpdateEvent?.Invoke();

            var unitTasks = new List<Task>();
            foreach (var unit in Units)
            {
                unitTasks.Add(Task.Run(unit.Update));
            }
            await Task.WhenAll(unitTasks);

            if (ConfigGameServer.IsDebugGameUpdate)
            {
                GameViewer.ViewFullInfo(this);
            }

            if (ConfigGameServer.IsDebugChunkStatus)
            {
                DebugPrintChunkStatus();
            }
        }

        private void DebugPrintChunkStatus()
        {
            // ... (прежняя реализация вывода статуса чанков)
        }

        public async Task EndAsync()
        {
            if (!_isPlaying) return;

            _logger.LogInformation("Ending game {GameId}", Id);
            _isPlaying = false;
            _cts.Cancel();

            BattleManager.Games.Remove(this);

            var endTasks = new List<Task>();
            foreach (var player in Players)
            {
                var userTcp = BattleManager.GameServer.TcpServer.GetClientByUserAuth(player.UserAuth);
                if (userTcp != null)
                {
                    endTasks.Add(new EndGameSender(userTcp).SendAsync());
                    player.UserAuth.Status.SetInPassive();
                }
            }

            await Task.WhenAll(endTasks);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _logger.LogInformation("Disposing game {GameId}", Id);
            _cts.Cancel();
            _cts.Dispose();
            EndAsync().GetAwaiter().GetResult();
        }
    }
}