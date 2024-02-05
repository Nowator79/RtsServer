using RtsServer.App.Buttle.Constructions;
using RtsServer.App.Buttle.MapButtle;
using RtsServer.App.Buttle.Units;
using RtsServer.App.DataBase.Dto;

namespace RtsServer.App.Buttle
{
    public class ButtleManager
    {
        public List<Game> Games { get; private set; }
        public HashSet<UserAuth> UsersForSearching { get; private set; }
        public GameServer GameServer { get; private set; }
        public MapSceneFactory MapSceneFactory { get; private set; }
        public Dictionary<string, MapScene> MapScene { get; private set; }

        private readonly string[] MapsForPvp = { "test" };
        private readonly Random rnd = new();

        public ButtleManager(GameServer gameServer)
        {
            GameServer = gameServer;
            Games = new();
            UsersForSearching = new();
            MapSceneFactory = new();
            MapScene = new();

            if (ConfigGameServer.IsDebugStartGame)
            {
                StartSingleGame();
            }
        }

        public void AddGame(Game Game)
        {
            Games.Add(Game);
        }

        public void FindUsersForButtle()
        {
            if (UsersForSearching.Count >= 2)
            {
                UserAuth firstUser = null;
                UserAuth secondUser = null;
                foreach (UserAuth user in UsersForSearching)
                {
                    if (firstUser == null)
                    {
                        firstUser = user;
                    }
                    else
                    {
                        secondUser = user;
                        break;
                    }
                }

                UsersForSearching.Remove(firstUser);
                UsersForSearching.Remove(secondUser);

                Game game = new(Games.Count, this);
                game.Players.Add(new Player(firstUser));
                game.Players.Add(new Player(secondUser));

                Unit soldier = new Soldier(new Dto.Vector2Float());
                soldier.SetTargetPosition(new Dto.Vector2Int(1, 9));
                game.AddUnit(soldier);

                game
                //    .SetMap(MapAdapter.Get(MapSceneFactory.LoadMapByName("sad")))
                    .Start();

                AddGame(game);
            }
        }

        public void FindUserOneForButtle()
        {
            if (UsersForSearching.Count >= 1)
            {
                UserAuth firstUser = null;
                foreach (UserAuth user in UsersForSearching)
                {
                    if (firstUser == null)
                    {
                        firstUser = user;
                        break;
                    }
                }

                UsersForSearching.Remove(firstUser);

                Game game = new(Games.Count, this);
                string nameMap = MapsForPvp[rnd.Next(0, MapsForPvp.Length)];
                List<MapScene> mapScenes = new(MapSceneFactory.GetAllMapScene());
                MapScene? mapScene = mapScenes.Find(mapScene => mapScene.Map.Code == nameMap);
                if (mapScene == null) throw new Exception("Не найдена нужная карта");
                game.SetMap(mapScene.Map);
                game.Players.Add(new Player(firstUser));

                foreach (Unit unit in mapScene.Units)
                {
                    unit.SetGame(game);
                    game.AddUnit(unit);
                }

                foreach (Construction construction in mapScene.ConstructionAdditionalsForMap)
                {
                    construction.SetGame(game);
                    game.AddConstruction(construction);
                }

                game.Start();

                AddGame(game);
            }
        }

        public void AddUserForSearch(UserAuth user)
        {
            UsersForSearching.Add(user);
            //FindUsersForButtle();
            FindUserOneForButtle();
        }

        public int StartSingleGame()
        {
            Game game = new(Games.Count, this);
            //game.SetMap(MapAdapter.Get(MapSceneFactory.LoadMapByName("test")));

            Unit soldier = new Soldier(new Dto.Vector2Float());
            soldier.SetGame(game);
            soldier.SetTargetPosition(new Dto.Vector2Int(45, 45));
            game.AddUnit(soldier);

            Unit tank = new TankT1(new Dto.Vector2Float(4, 4));
            tank.SetGame(game);
            game.AddUnit(tank);

            game.Start();

            AddGame(game);
            return Games.FindIndex(x => x == game);
        }
    }
}
