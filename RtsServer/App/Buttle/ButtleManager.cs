
using RtsServer.App.Adapters;
using RtsServer.App.Buttle.Units;
using RtsServer.App.DataBase.Dto;
using RtsServer.App.FileSystem;
using RtsServer.App.FileSystem.Dto;

namespace RtsServer.App.Buttle
{
    public class ButtleManager
    {
        public List<Game> Games { get; private set; }

        public HashSet<UserAuth> usersForSearching { get; private set; }

        public GameServer GameServer { get; private set; }
        public MapFileManager mapFileManager { get; private set; }
        public ButtleManager(GameServer gameServer)
        {
            GameServer = gameServer;
            Games = new();
            usersForSearching = new();
            mapFileManager = new();
            //StartSingleGame();
        }

        public void AddGame(Game Game)
        {
            Games.Add(Game);
        }

        public void FindUsersForButtle()
        {
            if (usersForSearching.Count >= 2)
            {
                UserAuth firstUser = null;
                UserAuth secondUser = null;
                foreach (UserAuth user in usersForSearching)
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

                usersForSearching.Remove(firstUser);
                usersForSearching.Remove(secondUser);

                Game game = new(Games.Count, this);
                game.Players.Add(new Player(firstUser));
                game.Players.Add(new Player(secondUser));

                Unit soldier = new Soldier(new Dto.Vector2Float());
                soldier.SetTargetPosition(new Dto.Vector2Int(1, 9));
                game.AddUnit(soldier);

                game
                    .SetMap(MapAdapter.Get(mapFileManager.LoadMapByName("sad")))
                    .Start();

                AddGame(game);
            }
        }

        public void FindUserOneForButtle()
        {
            if (usersForSearching.Count >= 1)
            {
                UserAuth firstUser = null;
                foreach (UserAuth user in usersForSearching)
                {
                    if (firstUser == null)
                    {
                        firstUser = user;
                        break;
                    }
                }

                usersForSearching.Remove(firstUser);

                Game game = new(Games.Count, this);
                game.SetMap(MapAdapter.Get(mapFileManager.LoadMapByName("test")));

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
            usersForSearching.Add(user);
            //FindUsersForButtle();
            FindUserOneForButtle();
        }

        public int StartSingleGame()
        {
            Game game = new(Games.Count, this);
            game.SetMap(MapAdapter.Get(mapFileManager.LoadMapByName("test")));

            Unit soldier = new Soldier(new Dto.Vector2Float());
            soldier.SetGame(game);
            soldier.SetTargetPosition(new Dto.Vector2Int(1, 9));
            game.AddUnit(soldier);

            game.Start();

            AddGame(game);
            return Games.FindIndex(x => x == game);
        }
    }
}
