using Microsoft.Extensions.Logging;
using RtsServer.App.Battle.MapBattle;
using RtsServer.App.DataBase.Dto;

namespace RtsServer.App.Battle
{
    public class BattleManager
    {
        private readonly ILogger<BattleManager> _logger;
        public List<Game> Games { get; private set; }
        public GameServer GameServer { get; private set; }
        public MapSceneFactory MapSceneFactory { get; private set; }
        public Dictionary<string, MapScene> MapScene { get; private set; }

        public Dictionary<string, MatchType> MatchTypes { get; } = new();
        public Dictionary<string, List<UserAuth>> MatchmakingQueues { get; } = new();

        private readonly Random rnd = new();
        private CancellationToken _cancellationToken;

        public BattleManager(GameServer gameServer, CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;

            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            _logger = loggerFactory.CreateLogger<BattleManager>();

            GameServer = gameServer;
            Games = new();
            MapSceneFactory = new();
            MapScene = new();

            InitMatchTypes();
        }
        
        private void InitMatchTypes()
        {
            MatchTypes["1v1"] = new MatchType("1v1", 2, ["test"]);
            MatchTypes["demo"] = new MatchType("demo", 1, ["test"]);
            MatchTypes["ffa4"] = new MatchType("ffa4", 4, ["test"]);
        }

        public void AddUserToQueue(Queue queueData)
        {
            if (!MatchTypes.ContainsKey(queueData.Type))
            {
                _logger.LogError($"Тип матча '{queueData.Type}' не найден.");
                return;
            }

            if (!MatchmakingQueues.ContainsKey(queueData.Type))
                MatchmakingQueues[queueData.Type] = new();

            List<UserAuth> queue = MatchmakingQueues[queueData.Type];
            if (!queue.Contains(queueData.User))
                queue.Add(queueData.User);

            TryStartMatch(queueData.Type);
        }

        private void TryStartMatch(string matchCode)
        {
            var matchType = MatchTypes[matchCode];
            var queue = MatchmakingQueues[matchCode];

            if (queue.Count < matchType.PlayersRequired)
                return;

            var players = queue.Take(matchType.PlayersRequired).ToList();
            queue.RemoveRange(0, matchType.PlayersRequired);

            var mapScenes = MapSceneFactory.GetAllMapScene()
                .Where(scene => matchType.AllowedMapCodes.Contains(scene.Map.Code))
                .ToList();

            if (mapScenes.Count == 0)
            {
                _logger.LogError($"Нет подходящих карт для матча {matchCode}");
                return;
            }

            var selectedMap = mapScenes[rnd.Next(mapScenes.Count)];

            var game = new Game(Games.Count, this, _cancellationToken);
            game.SetMap(selectedMap.Map);

            int id = 0;
            foreach (var user in players)
                game.Players.Add(new Player(user, id++, game));

            foreach (var unit in selectedMap.Units)
            {
                unit.Init(game);
                game.AddUnit(unit);
            }

            foreach (var construction in selectedMap.ConstructionAdditionalsForMap)
            {
                construction.SetGame(game);
                game.AddConstruction(construction);
            }

            game.Init();

            AddGame(game);
        }

        public void AddGame(Game game)
        {
            Games.Add(game);
        }

        public void EndBattleByUser(UserAuth user)
        {
            Game? game = Games.Find(game => game.Players.Any(player => player.UserAuth == user));
            game?.EndAsync();
        }

        public void RemoveUserForSearch(UserAuth user)
        {
            foreach (List<UserAuth> queue in MatchmakingQueues.Values)
            {
                queue.RemoveAll(u => u == user);
            }
        }
    }

    public struct Queue(UserAuth user, string type)
    {
        public UserAuth User = user;
        public string Type = type;
    }
}
