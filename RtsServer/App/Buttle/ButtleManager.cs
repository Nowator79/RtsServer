
using RtsServer.App.Adapters;
using RtsServer.App.Buttle.Units;
using RtsServer.App.DataBase.Dto;
using RtsServer.App.FileSystem;
using RtsServer.App.FileSystem.Dto;
using RtsServer.App.ViewConsole;

namespace RtsServer.App.Buttle
{
    public class ButtleManager
    {
        public List<Game> Games { get; private set; }
        public HashSet<UserAuth> UsersForSearching { get; private set; }
        public GameServer GameServer { get; private set; }
        public MapFileManager MapFileManager { get; private set; }
        private readonly string[] MapsForPvp = { "sad", "test" };
        private readonly Random rnd = new();

        public ButtleManager(GameServer gameServer)
        {
            GameServer = gameServer;
            Games = new();
            UsersForSearching = new();
            MapFileManager = new();
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
                    .SetMap(MapAdapter.Get(MapFileManager.LoadMapByName("sad")))
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
                //game.SetMap(MapAdapter.Get(mapFileManager.LoadMapByName(nameMap)));
                game.SetMap(MapAdapter.Get(MapFileManager.LoadMapByName("test")));
                game.Players.Add(new Player(firstUser));

                Unit soldier = new Soldier(new Dto.Vector2Float());
                soldier.SetGame(game);
                soldier.SetTargetPosition(new Dto.Vector2Int(1, 9));
                game.AddUnit(soldier);

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
            game.SetMap(MapAdapter.Get(MapFileManager.LoadMapByName("test")));

            Unit soldier = new Soldier(new Dto.Vector2Float());
            soldier.SetGame(game);
            soldier.SetTargetPosition(new Dto.Vector2Int(45, 45));
            game.AddUnit(soldier);

            game.Start();

            AddGame(game);
            return Games.FindIndex(x => x == game);
        }
    }
}
