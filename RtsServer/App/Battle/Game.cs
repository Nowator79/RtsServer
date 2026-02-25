using Microsoft.Extensions.Logging;
using RtsServer.App.Adapters;
using RtsServer.App.Battle.Constructions;
using RtsServer.App.Battle.Dto;
using RtsServer.App.Battle.Interfaces;
using RtsServer.App.Battle.MapBattle;
using RtsServer.App.Battle.Navigator;
using RtsServer.App.Battle.Units;
using RtsServer.App.NetWorkDto.Response;
using RtsServer.App.NetWorkResponseSender;
using RtsServer.App.Tools;
using System.Numerics;
using static RtsServer.App.Battle.Player;

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
        private int _missileNextId = 0;
        private int _constructionNextId = 0;
        bool _isPlaying = false;

        public int Id { get; }
        public long CreateDateTime { get; }
        public Map? Map { get; private set; }
        public List<Player> Players { get; } = [];
        public List<Unit> Units { get; } = [];
        public List<Missile> Missiles { get; } = [];
        public List<Construction> Constructions { get; } = [];
        public HashSet<Unit> UnitsForAdd { get; } = [];
        public HashSet<Missile> MissilesForAdd { get; } = [];
        public HashSet<Construction> ConstructionsForAdd { get; } = [];
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

        public void Init()
        {
            SetStatusUsersInGame();
        }

        private const int StartingResources = 300;

        public void Start()
        {
            if (Map == null)
                throw new InvalidOperationException("Map not set");

            _logger.LogInformation("Starting game {GameId}", Id);
            _isPlaying = true;

            GivePlayersStartingResources();

            SendUpdateGameAsync();

            UpdateEvent += SendUpdateGameAsync;
            UpdateEvent += TimeSystem.Update;

            _ = Task.Run(GameLoopAsync, _cts.Token);
        }

        public void TryStart()
        {
            if(!Players.Where(p => p.PlayerState != PlayerStateType.Ready).Any())
            {
                Start();
            }
        }

        private void GivePlayersStartingResources()
        {
            foreach (Player player in Players)
            {
                IResourceStorage? storage = Constructions
                    .Where(c => c.OwnerId == player.Id)
                    .OfType<IResourceStorage>()
                    .FirstOrDefault();
                if (storage != null)
                {
                    storage.AddResources(StartingResources);
                    _logger.LogDebug("Player {PlayerId} получил стартовые ресурсы: {Amount}", player.Id, StartingResources);
                }
            }
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

        private async void SetStatusUsersInGame()
        {
            foreach (Player player in Players)
            {
                player.UserAuth.Status.SetInGame();
            }

            foreach (Player player in Players)
            {
                NetWork.Tcp.UserClientTcp? userTcp = BattleManager.GameServer.TcpServer.GetClientByUserAuth(player.UserAuth);
                if (userTcp != null)
                {
                    await new StartGameSender(userTcp)
                        .SetDate(new SetStartGameData(Map.Code, player.Id, new Vector2Int()))
                        .SendAsync();
                }
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

            ClearConstructions();
            ClearMissile();
            ClearUnits();

            List<Task> update = [];

            foreach (Unit unit in Units)
            {
                update.Add(Task.Run(unit.Update));
            }

            foreach (Missile missile in Missiles)
            {
                update.Add(Task.Run(missile.Update));
            }

            foreach (Construction construction in Constructions)
            {
                update.Add(Task.Run(construction.Update));
            }

            await Task.WhenAll(update);

            UpdatePlayersStats();
        }

        private void ClearConstructions()
        {
            foreach (Construction construction in ConstructionsForAdd)
            {
                construction.SetId(_constructionNextId++);
                Constructions.Add(construction);
                _logger.LogDebug("Construction {ConstructionId} added to game {GameId}", construction.Id, Id);
            }
            ConstructionsForAdd.Clear();

            Constructions.Where(c => c.IsDestroyed)
            .ToList()
            .ForEach(RemoveEntity);
        }
        private void ClearMissile()
        {
            foreach (Missile missile in MissilesForAdd)
            {
                Missiles.Add(missile);
            }
            MissilesForAdd.Clear();

            Missiles.Where(m => m.IsDestroyed)
            .ToList()
            .ForEach(RemoveEntity);
        }
        private void ClearUnits()
        {
            foreach (Unit unit in UnitsForAdd)
            {
                Units.Add(unit);
            }
            UnitsForAdd.Clear();

            Units.Where(u => u.IsDestroyed)
            .ToList()
            .ForEach(RemoveEntity);
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

        public void AddMissile(Missile missile)
        {
            missile.SetId(_missileNextId++);
            MissilesForAdd.Add(missile);
        }

        private void RemoveEntity(BattleEntity entity)
        {
            if (entity is Unit unit)
                Units.Remove(unit);
            else if (entity is Construction construction)
                Constructions.Remove(construction);
            else if (entity is Missile missile)
                Missiles.Remove(missile);

            entity.Dispose();
        }

        private void UpdatePlayersStats()
        {
            List<Task> tasks = [];
            Players.ForEach(p =>
            {
                NetWork.Tcp.UserClientTcp? userTcp = BattleManager.GameServer.TcpServer.GetClientByUserAuth(p.UserAuth);
                if (userTcp == null) return;
                tasks.Add(new PlayerStateSender(userTcp).SetDate(PlayerStateAdapter.Get(p.GetState())).SendAsync());
            });
        }

        public void TryBuildConstruction(string code, Vector2Int position, int playerId)
        {
            if (!Players.Any(p => p.Id == playerId))
            {
                _logger.LogWarning("Player {PlayerId} not found in game {GameId}", playerId, Id);
                return;
            }

            int cost = ConstructionFactory.GetBuildCost(code);
            if (cost <= 0)
            {
                _logger.LogWarning("Unknown building code or zero cost: {Code}", code);
                return;
            }

            int totalResources = Players.First(p => p.Id == playerId).GetState().Resources;
            if (totalResources < cost)
            {
                _logger.LogWarning("Player {PlayerId} has insufficient resources: {Resources}, need {Cost} for {Code}", playerId, totalResources, cost, code);
                return;
            }

            if (!TrySpendPlayerResources(playerId, cost))
            {
                _logger.LogWarning("Failed to spend resources for player {PlayerId}", playerId);
                return;
            }

            try
            {
                Construction construction = ConstructionFactory.GetByCode(code, position, playerId);
                construction.SetGame(this);
                ConstructionsForAdd.Add(construction);

                _logger.LogInformation("Player {PlayerId} строит здание {Code} на {Pos} (потрачено {Cost})", playerId, code, position, cost);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при строительстве здания {Code} игроком {PlayerId}. Возврат ресурсов.", code, playerId);
                RefundPlayerResources(playerId, cost);
            }
        }

        private void RefundPlayerResources(int playerId, int amount)
        {
            float remaining = amount;
            foreach (Construction c in Constructions.Where(c => c.OwnerId == playerId))
            {
                if (c is not IResourceStorage storage || remaining <= 0) continue;
                float add = Math.Min(remaining, storage.LimitResources - storage.Resources);
                if (add <= 0) continue;
                storage.AddResources(add);
                remaining -= add;
                if (remaining <= 0) return;
            }
        }

        /// <summary>Списать ресурсы игрока с его хранилищ (штабов и т.д.). Возвращает true, если списание выполнено.</summary>
        private bool TrySpendPlayerResources(int playerId, int amount)
        {
            float remaining = amount;
            foreach (Construction c in Constructions.Where(c => c.OwnerId == playerId))
            {
                if (c is not IResourceStorage storage || remaining <= 0)
                    continue;
                float take = Math.Min(remaining, storage.Resources);
                if (take <= 0) continue;
                if (storage.TrySpend(take))
                    remaining -= take;
                if (remaining <= 0)
                    return true;
            }
            return remaining <= 0;
        }

    }
}