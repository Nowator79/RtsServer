using RtsServer.App.Adapters;
using RtsServer.App.Battle.Chat;
using RtsServer.App.Battle.Constructions;
using RtsServer.App.Battle.Dto;
using RtsServer.App.Battle.MapButlle;
using RtsServer.App.Battle.Navigator;
using RtsServer.App.Battle.Units;
using RtsServer.App.NetWork.Tcp;
using RtsServer.App.NetWorkDto;
using RtsServer.App.NetWorkDto.Response;
using RtsServer.App.NetWorkResponseSender;
using RtsServer.App.Tools;
using RtsServer.App.ViewConsole;

namespace RtsServer.App.Battle
{
    public class Game
    {
        public int Id { get; set; }
        public long CreateDateTime { get; private set; }
        public Map Map { get; private set; }
        public List<Player> Players { get; set; } = new List<Player> { };
        public List<Unit> Units { get; set; } = new();
        public List<Construction> Constructions { get; set; } = new();
        public BattleManager BattleManager { get; private set; }
        public TimeSystem TimeSystem { get; private set; }
        public Action ActionsUpdate { private get; set; }

        private readonly GroundUnitNavigator gNav;

        private bool IsPlay = false;


        public Game(int Id, BattleManager battleManager)
        {
            this.Id = Id;
            BattleManager = battleManager;
            CreateDateTime = DateTime.Now.Ticks;
            TimeSystem = new();
            gNav = new();
        }

        public Game SetMap(Map map)
        {
            Map = map;
            gNav.SetMap(map);
            return this;
        }

        public void Start()
        {
            if (Map == null)
            {
                throw new Exception("Не задан Map");
            }


            IsPlay = true;

            foreach (Player player in Players)
            {
                UserClientTcp? userTcp = BattleManager.GameServer.TcpServer.GetClientByUserAuth(player.UserAuth);

                if (userTcp != null)
                {
                    new StartGameSender(userTcp).SetDate(new SetStartGameData(Map.Code, new Vector2Int())).SendMessage();
                }
            }
            Console.WriteLine("Старт");
            SendUpdateGame();
            ActionsUpdate += SendUpdateGame;
            ActionsUpdate += TimeSystem.Update;

            SetStatusUsersInGame();

            Task.Run(Loop);
        }

        public void SendUpdateGame()
        {
            foreach (Player player in Players)
            {
                UserClientTcp? userTcp = BattleManager.GameServer.TcpServer.GetClientByUserAuth(player.UserAuth);

                if (userTcp != null)
                {
                    MainResponse responseGame = new("battle", "/gameBattle/setGame/", "200");
                    NGame nGame = GameAdapter.Get(this);
                    responseGame.SetBody(nGame);
                    userTcp.Write(responseGame);
                }
            }
        }

        private void SetStatusUsersInGame()
        {
            foreach (Player user in Players)
            {
                user.UserAuth.Status.SetInGame();
            }
        }

        /**
         * to do:
         * Сделать приватным, т.к. 
         * внешние системы не отвечают 
         * за создание юнитов в контексте игры
         */
        private int UnitNextId = 0;
        public void AddUnit(Unit unit)
        {
            unit.SetId(UnitNextId++);
            Units.Add(unit);
        }

        private int ConstructionNextId = 0;
        public void AddConstruction(Construction construction)
        {
            construction.SetId(ConstructionNextId++);
            Constructions.Add(construction);
        }

        public void Update()
        {
            ActionsUpdate();

            List<Task> tasks = new();

            // Движение к цели
            Units.ForEach(unit =>
            {
                tasks.Add(Task.Run(unit.Update));
            });
            Task.WaitAll(tasks.ToArray());

            if (ConfigGameServer.IsDebugGameUpdate)
            {
                GameViewer.ViewFullInfo(this);
                if (ConfigGameServer.IsEnabledClearConsole) Console.Clear();
            }


            if (ConfigGameServer.IsDebugChunkStatus)
            {
                int[,] viewConsoleArray = new int[Map.Width, Map.Length];
                for (int x = 0; x < Map.Width; x++)
                {
                    for (int y = 0; y < Map.Length; y++)
                    {
                        viewConsoleArray[x, y] = Map.Chunks[x * Map.Length + y].UnitsInPoint.Count;
                    }
                }

                for (int x = 0; x < Map.Width; x++)
                {
                    for (int y = 0; y < Map.Length; y++)
                    {
                        Console.Write($"{viewConsoleArray[x, y],2}");
                    }
                    Console.WriteLine();
                }
                Console.WriteLine("=============");
                Console.WriteLine();


                if (ConfigGameServer.IsEnabledClearConsole) Console.Clear();
            }
        }

        public void End()
        {
            IsPlay = false;
            BattleManager.Games.Remove(this);
            foreach (Player? palyer in Players)
            {
                UserClientTcp? userTcp = BattleManager.GameServer.TcpServer.GetClientByUserAuth(palyer.UserAuth);
                if (userTcp != null)
                {
                    new EndGameSender(userTcp).SendMessage();
                    palyer.UserAuth.Status.SetInPassive();
                }
            }
        }

        private void Loop()
        {
            while (IsPlay)
            {
                Update();
                Thread.Sleep(50);
            }
        }
    }
}
