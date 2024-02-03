using RtsServer.App.Adapters;
using RtsServer.App.Buttle.Dto;
using RtsServer.App.Buttle.MapButlle;
using RtsServer.App.Buttle.Navigator;
using RtsServer.App.Buttle.Units;
using RtsServer.App.NetWork.Tcp;
using RtsServer.App.NetWorkDto;
using RtsServer.App.NetWorkDto.Response;
using RtsServer.App.NetWorkResponseSender;
using RtsServer.App.Tools;
using RtsServer.App.ViewConsole;

namespace RtsServer.App.Buttle
{
    public class Game
    {
        public int Id { get; set; }
        public long createDateTime { get; private set; }
        public Map Map { get; private set; }
        public List<Player> Players { get; set; } = new List<Player> { };
        public List<Unit> Units { get; set; } = new List<Unit>();
        public List<Action> ActionsUpdate { private get; set; } = new();
        public ButtleManager buttleManager { get; private set; }

        private GroundUnitNavigator gNav;

        private bool IsPlay = false;
        private Random _random;
        public TimeSystem TimeSystem { get; private set; }

        public Game(int Id, ButtleManager buttleManager)
        {
            this.Id = Id;
            this.buttleManager = buttleManager;
            createDateTime = DateTime.Now.Ticks;
            TimeSystem = new();

            gNav = new();
        }

        public Game SetMap(Map map)
        {
            this.Map = map;
            gNav.SetMap(map);
            return this;
        }


        public void Start()
        {
            if (Map == null)
            {
                throw new Exception("Не задан Map");
            }

            _random = new Random();

            IsPlay = true;

            foreach (Player player in Players)
            {
                UserClientTcp? userTcp = buttleManager.GameServer.TcpServer.GetClientByUserAuth(player.UserAuth);

                if (userTcp != null)
                {
                    new StartGameSender(userTcp).SetDate(new SetStartGameData(Map.Code, new Vector2Int())).SendMessage();
                }
            }

            SendUpdateGame();
            ActionsUpdate.Add(SendUpdateGame);
            ActionsUpdate.Add(TimeSystem.Update);

            SetStatusUsersInGame();

            Task.Run(Loop);
        }

        public void SendUpdateGame()
        {
            foreach (Player player in Players)
            {
                UserClientTcp? userTcp = buttleManager.GameServer.TcpServer.GetClientByUserAuth(player.UserAuth);

                if (userTcp != null)
                {
                    MainResponse responseGame = new("buttle", "/gameBattle/setGame/", "200");
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
        public void AddUnit(Unit unit)
        {
            unit.SetId(Units.Count);
            Units.Add(unit);
        }

        public void Update()
        {
            foreach (Action action in ActionsUpdate)
            {
                action();
            }

            // Движение к цели
            Units.ForEach(unit =>
            {
                unit.MoveToTarget();
            });

            if (ConfigGameServer.IsDebugGameUpdate)
            {
                GameViewer.ViewFullInfo(this);
                Console.Clear();
            }
        }

        public void End()
        {
            IsPlay = false;
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
