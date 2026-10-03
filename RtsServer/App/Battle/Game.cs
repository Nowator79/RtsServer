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
using System.Text;
using System.Text.Json;
using System.Threading;
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
        private readonly object _lifecycleLock = new();
        private bool _disposed;
        private int _unitNextId = 0;
        private int _missileNextId = 0;
        private int _constructionNextId = 0;
        bool _isPlaying = false;
        private bool _victoryHandled;
        private bool _endResultSent;
        private int _victoryWinnerId = -1;
        private double _victoryEndTimer = -1;
        /// <summary>1 = идёт отправка полного снапшота; пропускаем тики, чтобы не копить очередь в TCP.</summary>
        private int _gameSyncInFlight;
        private int _gameSyncPending;
        private int _playerStatsInFlight;
        /// <summary>Счётчик тиков до сетевого синка (2 ≈ 10 Гц при Delay 50ms).</summary>
        private int _ticksSinceGameSync;

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
        public BattleManager? BattleManager { get; }
        public TimeSystem TimeSystem { get; }

        /// <summary>Headless-симуляция без сети / BattleManager.</summary>
        public bool IsSimulation { get; }

        public event Action UpdateEvent;
        private CancellationToken _cancellationToken;

        public static Game CreateSimulation(int id = 0)
        {
            return new Game(id, battleManager: null, CancellationToken.None, isSimulation: true);
        }

        public Game(int id, BattleManager battleManager, CancellationToken cancellationToken)
            : this(id, battleManager, cancellationToken, isSimulation: false)
        {
        }

        private Game(int id, BattleManager? battleManager, CancellationToken cancellationToken, bool isSimulation)
        {
            Id = id;
            BattleManager = battleManager;
            IsSimulation = isSimulation;
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

        private int _startingResources = MapScene.DefaultStartingResources;
        private Dictionary<int, Vector2Int> _cameraStartsByPlayer = new();

        public Game ApplySceneSettings(MapScene scene)
        {
            if (scene == null) return this;
            _startingResources = scene.StartingResources > 0
                ? scene.StartingResources
                : MapScene.DefaultStartingResources;
            _cameraStartsByPlayer = scene.CameraStartsByPlayer != null
                ? new Dictionary<int, Vector2Int>(scene.CameraStartsByPlayer)
                : new Dictionary<int, Vector2Int>();
            return this;
        }

        public void Start()
        {
            if (Map == null)
                throw new InvalidOperationException("Map not set");

            lock (_lifecycleLock)
            {
                if (_isPlaying)
                {
                    _logger.LogDebug("Game {GameId} is already running; duplicate Start() ignored", Id);
                    return;
                }

                _isPlaying = true;
            }

            _logger.LogInformation("Starting game {GameId}", Id);

            GivePlayersStartingResources();
            Players.ForEach(p => p.PlayerState = PlayerStateType.Playing);

            QueueGameSync();

            UpdateEvent += TimeSystem.Update;

            _ = Task.Run(GameLoopAsync, _cts.Token);
        }

        public void TryStart()
        {
            if (_isPlaying)
                return;

            if (!Players.Where(p => p.PlayerState != PlayerStateType.Ready).Any())
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
                    storage.AddResources(_startingResources);
                    _logger.LogDebug("Player {PlayerId} получил стартовые ресурсы: {Amount}", player.Id, _startingResources);
                }
            }
        }

        /// <summary>
        /// Ставит в очередь снимок. Сериализация JSON уходит в ThreadPool —
        /// раньше GameAdapter.Get + Serialize шли синхронно внутри UpdateEvent и подвешивали симуляцию.
        /// </summary>
        private void QueueGameSync()
        {
            if (!_isPlaying || IsSimulation || BattleManager?.GameServer == null)
                return;

            Interlocked.Exchange(ref _gameSyncPending, 1);
            if (Interlocked.CompareExchange(ref _gameSyncInFlight, 1, 0) != 0)
                return;

            PumpGameSync();
        }

        private void PumpGameSync()
        {
            Interlocked.Exchange(ref _gameSyncPending, 0);

            NetWorkDto.NGame nGame;
            try
            {
                nGame = GameAdapter.Get(this);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Game {GameId}: ошибка сборки снапшота", Id);
                Interlocked.Exchange(ref _gameSyncInFlight, 0);
                return;
            }

            _ = SendNGameAsync(nGame);
        }

        private async Task SendNGameAsync(NetWorkDto.NGame nGame)
        {
            try
            {
                byte[] bytes = await Task.Run(() =>
                {
                    MainResponse response = new MainResponse("battle", "/gameBattle/setGame/", "", "200")
                        .SetBody(nGame);
                    return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(response) + "\n");
                }).ConfigureAwait(false);

                if (!_isPlaying || BattleManager?.GameServer == null)
                    return;

                var tasks = new List<Task>();
                foreach (var player in Players)
                {
                    if (player.IsBot)
                        continue;

                    var userTcp = BattleManager.GameServer.TcpServer.GetClientByUserAuth(player.UserAuth);
                    if (userTcp != null)
                        tasks.Add(userTcp.WriteBytesAsync(bytes, _cancellationToken));
                }

                if (tasks.Count > 0)
                    await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Game {GameId}: ошибка отправки снапшота", Id);
            }
            finally
            {
                Interlocked.Exchange(ref _gameSyncInFlight, 0);
                // Повтор только из игрового цикла (QueueGameSync), не с фонового потока.
            }
        }

        private async void SetStatusUsersInGame()
        {
            if (IsSimulation || BattleManager?.GameServer == null)
                return;

            foreach (Player player in Players)
            {
                player.UserAuth.Status.SetInGame();
            }

            foreach (Player player in Players)
            {
                NetWork.Tcp.UserClientTcp? userTcp = BattleManager.GameServer.TcpServer.GetClientByUserAuth(player.UserAuth);
                if (userTcp != null)
                {
                    Vector2Int cameraStart = _cameraStartsByPlayer.TryGetValue(player.Id, out Vector2Int pos)
                        ? pos
                        : new Vector2Int(0, 0);

                    await new StartGameSender(userTcp)
                        .SetDate(new SetStartGameData(Map.Code, player.Id, cameraStart))
                        .SendAsync();
                }
            }
        }

        /// <summary>
        /// Один тик headless-симуляции (без сети). Вызывать после EnableFixedStep.
        /// </summary>
        public void SimulationTick()
        {
            if (Map == null)
                throw new InvalidOperationException("Map not set");

            TimeSystem.Update();
            ClearUnits();
            ClearMissile();
            ClearConstructions();

            foreach (Unit unit in Units)
                unit.Update();

            foreach (Missile missile in Missiles)
                missile.Update();

            foreach (Construction construction in Constructions)
                construction.Update();
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
            // Сначала убрать уничтоженное, потом синк — клиент увидит отсутствие штаба.
            ClearConstructions();
            ClearMissile();
            ClearUnits();

            UpdateEvent?.Invoke();

            RecalcPower();

            double dt = TimeSystem.GetDelta();
            foreach (Player player in Players)
                player.Ai?.Tick(this, player, dt);

            // Все сущности обновляем в одном потоке игрового цикла:
            // unit.Update() меняет общие коллекции карты (UnitsInPoint), и при
            // параллельном Task.Run это приводит к гонкам/порче состояния коллекций.
            foreach (Unit unit in Units)
            {
                unit.Update();
            }

            foreach (Missile missile in Missiles)
            {
                missile.Update();
            }

            foreach (Construction construction in Constructions)
            {
                construction.Update();
            }

            CheckVictoryCondition();

            // Не ждём сеть в игровом цикле — иначе при забитом TCP симуляция тормозит вместе с отправкой.
            SendPlayersStatsAsync();

            // Снимок после симуляции тика, не чаще ~10 Гц — иначе JSON блокирует/грузит цикл.
            _ticksSinceGameSync++;
            if (_ticksSinceGameSync >= 2)
            {
                _ticksSinceGameSync = 0;
                QueueGameSync();
            }
            else if (Volatile.Read(ref _gameSyncPending) == 1 && Volatile.Read(ref _gameSyncInFlight) == 0)
            {
                QueueGameSync();
            }
        }

        private bool HasLivingBuildings(int playerId) =>
            Constructions.Any(c => c.OwnerId == playerId && !c.IsDestroyed)
            || ConstructionsForAdd.Any(c => c.OwnerId == playerId && !c.IsDestroyed);

        /// <summary>
        /// У игрока не осталось ни одного здания — он проиграл.
        /// Остался один с зданиями — победа, через 5 с закрываем матч.
        /// </summary>
        private void CheckVictoryCondition()
        {
            if (!_isPlaying || Players.Count < 2)
                return;

            if (_victoryHandled)
            {
                if (_victoryEndTimer < 0)
                    return;
                _victoryEndTimer -= TimeSystem.GetDelta();
                if (_victoryEndTimer <= 0)
                {
                    _victoryEndTimer = -1;
                    _ = EndAsync();
                }
                return;
            }

            List<Player> withBuildings = Players.Where(p => HasLivingBuildings(p.Id)).ToList();
            if (withBuildings.Count == 1)
            {
                BeginVictory(withBuildings[0].Id);
            }
            else if (withBuildings.Count == 0)
            {
                BeginVictory(winnerPlayerId: -1);
            }
        }

        private void BeginVictory(int winnerPlayerId)
        {
            _victoryHandled = true;
            _victoryWinnerId = winnerPlayerId;
            _victoryEndTimer = 5.0;
            BroadcastMatchResult(winnerPlayerId);
            _logger.LogInformation(
                "Game {GameId} victory: winner={WinnerId}, end in 5s",
                Id, winnerPlayerId);
        }

        private void BroadcastMatchResult(int winnerPlayerId)
        {
            if (IsSimulation || BattleManager?.GameServer == null)
                return;

            _endResultSent = true;
            foreach (Player player in Players)
            {
                if (player.IsBot)
                    continue;

                var userTcp = BattleManager.GameServer.TcpServer.GetClientByUserAuth(player.UserAuth);
                if (userTcp == null || !userTcp.IsConnected())
                    continue;

                string result = winnerPlayerId < 0
                    ? "Draw"
                    : (player.Id == winnerPlayerId ? "Victory" : "Defeat");

                try
                {
                    // CancellationToken.None — пакет победы не должен сорваться при Cancel().
                    new EndGameSender(userTcp)
                        .SetDate(new EndGameResult(result, winnerPlayerId))
                        .SendAsync(CancellationToken.None)
                        .GetAwaiter()
                        .GetResult();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send match result to player {PlayerId}", player.Id);
                }
            }
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
                unit.SetId(_unitNextId++);
                unit.Init(this);
                Units.Add(unit);
                _logger.LogDebug("Unit {UnitId} spawned into game {GameId}", unit.Id, Id);
            }
            UnitsForAdd.Clear();

            Units.Where(u => u.IsDestroyed)
            .ToList()
            .ForEach(RemoveEntity);
        }

        /// <summary>
        /// Распределение энергии: потребители по Id (старые приоритетнее).
        /// Лишние относительно LimitEnergy отключаются (IsPowered = false).
        /// </summary>
        private void RecalcPower()
        {
            foreach (Player player in Players)
            {
                List<Construction> owned = Constructions
                    .Where(c => c.OwnerId == player.Id && !c.IsDestroyed && c.IsBuilt)
                    .ToList();

                int supply = 0;
                foreach (Construction c in owned)
                {
                    if (c is IEnergyProvider provider)
                        supply += provider.EnergyProvided;
                }

                // Сначала все онлайн; затем снимаем питание с потребителей сверх лимита.
                foreach (Construction c in owned)
                    c.IsPowered = true;

                int used = 0;
                foreach (Construction c in owned.OrderBy(c => c.Id))
                {
                    if (c is not IEnergyConsumer consumer) continue;
                    int need = consumer.EnergyRequired;
                    if (need <= 0)
                    {
                        c.IsPowered = true;
                        continue;
                    }

                    if (used + need <= supply)
                    {
                        c.IsPowered = true;
                        used += need;
                    }
                    else
                    {
                        c.IsPowered = false;
                    }
                }
            }
        }

        public void TryProduceUnit(int factoryId, string unitCode, int playerId)
        {
            if (!Players.Any(p => p.Id == playerId))
            {
                _logger.LogWarning("Player {PlayerId} not found in game {GameId}", playerId, Id);
                return;
            }

            Construction? construction = Constructions.FirstOrDefault(c => c.Id == factoryId && !c.IsDestroyed);
            if (construction is not IUnitFactory factory)
            {
                _logger.LogWarning("Factory {FactoryId} not found for player {PlayerId}", factoryId, playerId);
                return;
            }

            if (construction.OwnerId != playerId)
            {
                _logger.LogWarning("Factory {FactoryId} not owned by player {PlayerId}", factoryId, playerId);
                return;
            }

            if (!construction.IsBuilt || !construction.IsPowered)
            {
                _logger.LogWarning("Factory {FactoryId} offline or incomplete", factoryId);
                return;
            }

            if (!factory.TryGetRecipe(unitCode, out int cost, out float buildSeconds))
            {
                _logger.LogWarning("Unknown unit recipe: {UnitCode}", unitCode);
                return;
            }

            int totalResources = Players.First(p => p.Id == playerId).GetState().Resources;
            if (totalResources < cost)
            {
                _logger.LogWarning("Player {PlayerId} insufficient resources for {UnitCode}: {Have}/{Need}",
                    playerId, unitCode, totalResources, cost);
                return;
            }

            if (!TrySpendPlayerResources(playerId, cost))
            {
                _logger.LogWarning("Failed to spend resources for unit {UnitCode}", unitCode);
                return;
            }

            if (!factory.Enqueue(unitCode, buildSeconds))
            {
                RefundPlayerResources(playerId, cost);
                _logger.LogWarning("Factory {FactoryId} rejected enqueue {UnitCode}", factoryId, unitCode);
            }
        }

        public async Task EndAsync()
        {
            lock (_lifecycleLock)
            {
                if (!_isPlaying) return;
                _isPlaying = false;
            }

            _logger.LogInformation("Ending game {GameId}", Id);
            _cts.Cancel();
            UpdateEvent = null;

            BattleManager?.Games.Remove(this);

            if (IsSimulation || BattleManager?.GameServer == null)
                return;

            // Результат уже ушёл в BeginVictory — повторно не шлём (клиент ждёт 5с до лобби).
            if (!_endResultSent)
            {
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
            else
            {
                foreach (var player in Players)
                    player.UserAuth.Status.SetInPassive();
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _logger.LogInformation("Disposing game {GameId}", Id);
            EndAsync().GetAwaiter().GetResult();
            _cts.Dispose();
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

        private async void SendPlayersStatsAsync()
        {
            // После победы/остановки матча стейт уже не нужен — иначе Cancel() рвёт запись в fail.
            if (!_isPlaying || _victoryHandled || IsSimulation || BattleManager?.GameServer == null)
                return;

            if (Interlocked.CompareExchange(ref _playerStatsInFlight, 1, 0) != 0)
                return;

            try
            {
                List<Task> tasks = [];
                foreach (Player p in Players)
                {
                    if (p.IsBot) continue;
                    NetWork.Tcp.UserClientTcp? userTcp = BattleManager.GameServer.TcpServer.GetClientByUserAuth(p.UserAuth);
                    if (userTcp == null || !userTcp.IsConnected()) continue;
                    tasks.Add(new PlayerStateSender(userTcp).SetDate(PlayerStateAdapter.Get(p.GetState())).SendAsync(_cts.Token));
                }

                if (tasks.Count > 0)
                    await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Game {GameId}: ошибка отправки player stats", Id);
            }
            finally
            {
                Interlocked.Exchange(ref _playerStatsInFlight, 0);
            }
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

            if (!CanPlaceConstruction(code, position, playerId, out string placeError))
            {
                _logger.LogWarning("Нельзя поставить {Code} на {Pos}: {Reason}", code, position, placeError);
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
                float buildTime = ConstructionFactory.GetBuildTime(code);
                if (buildTime > 0f)
                    construction.BeginConstruction(buildTime);
                ConstructionsForAdd.Add(construction);

                _logger.LogInformation(
                    "Player {PlayerId} начал строительство {Code} на {Pos} (стоимость {Cost}, время {Time}с)",
                    playerId, code, position, cost, buildTime);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при строительстве здания {Code} игроком {PlayerId}. Возврат ресурсов.", code, playerId);
                RefundPlayerResources(playerId, cost);
            }
        }

        /// <summary>Макс. зазор в клетках до своего здания (Chebyshev).</summary>
        public const int BuildRangeCells = 10;

        /// <summary>
        /// Площадка: границы, ровный Height, не вода, свободно, в радиусе своих зданий.
        /// </summary>
        public bool CanPlaceConstruction(string code, Vector2Int position, int playerId, out string error)
        {
            error = "";
            if (Map == null)
            {
                error = "карта не задана";
                return false;
            }

            Vector2Int size = ConstructionFactory.GetSize(code);
            int sizeX = Math.Max(1, size.X);
            int sizeY = Math.Max(1, size.Y);

            if (position.X < 0 || position.Y < 0
                || position.X + sizeX > Map.Width
                || position.Y + sizeY > Map.Length)
            {
                error = "выход за границы карты";
                return false;
            }

            var chunks = Map.GetArrayMap();
            int baseHeight = chunks[position.X, position.Y].Height;

            for (int x = position.X; x < position.X + sizeX; x++)
            {
                for (int y = position.Y; y < position.Y + sizeY; y++)
                {
                    var chunk = chunks[x, y];
                    if (chunk.Id == 0)
                    {
                        error = "вода";
                        return false;
                    }
                    if (chunk.Height != baseHeight)
                    {
                        error = $"разный уровень клеток ({chunk.Height} != {baseHeight})";
                        return false;
                    }
                }
            }

            foreach (Construction existing in Constructions.Concat(ConstructionsForAdd))
            {
                if (FootprintsOverlap(position, size, existing.Position.ToInt(), existing.Size))
                {
                    error = "занято другим зданием";
                    return false;
                }
            }

            bool inRange = false;
            foreach (Construction existing in Constructions.Concat(ConstructionsForAdd))
            {
                if (existing.OwnerId != playerId) continue;
                int gap = FootprintGapCells(position, size, existing.Position.ToInt(), existing.Size);
                if (gap <= BuildRangeCells)
                {
                    inRange = true;
                    break;
                }
            }

            if (!inRange)
            {
                error = $"дальше {BuildRangeCells} клеток от своего здания";
                return false;
            }

            return true;
        }

        private static bool FootprintsOverlap(Vector2Int aPos, Vector2Int aSize, Vector2Int bPos, Vector2Int bSize)
        {
            return aPos.X < bPos.X + bSize.X
                && aPos.X + aSize.X > bPos.X
                && aPos.Y < bPos.Y + bSize.Y
                && aPos.Y + aSize.Y > bPos.Y;
        }

        private static int FootprintGapCells(Vector2Int aPos, Vector2Int aSize, Vector2Int bPos, Vector2Int bSize)
        {
            int gapX = 0;
            if (aPos.X + aSize.X <= bPos.X) gapX = bPos.X - (aPos.X + aSize.X);
            else if (bPos.X + bSize.X <= aPos.X) gapX = aPos.X - (bPos.X + bSize.X);

            int gapY = 0;
            if (aPos.Y + aSize.Y <= bPos.Y) gapY = bPos.Y - (aPos.Y + aSize.Y);
            else if (bPos.Y + bSize.Y <= aPos.Y) gapY = aPos.Y - (bPos.Y + bSize.Y);

            return Math.Max(gapX, gapY);
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