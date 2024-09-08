using RtsServer.App.Battle.Chat;
using RtsServer.App.Battle.Constructions;
using RtsServer.App.Battle.MapBattle;
using RtsServer.App.Battle.Units;
using RtsServer.App.DataBase.Dto;

namespace RtsServer.App.Battle
{
    public class BattleManager
    {
        public List<Game> Games { get; private set; }
        public HashSet<UserAuth> UsersForSearching { get; private set; }
        public GameServer GameServer { get; private set; }
        public MapSceneFactory MapSceneFactory { get; private set; }
        public Dictionary<string, MapScene> MapScene { get; private set; }

        private readonly string[] MapsForPvp = { "test" };
        private readonly Random rnd = new();
        public ChatSystem Chat { get; private set; }

        public BattleManager(GameServer gameServer)
        {
            GameServer = gameServer;
            Games = new();
            UsersForSearching = new();
            MapSceneFactory = new();
            MapScene = new();
            Chat = new();
        }

        public void AddGame(Game Game)
        {
            Games.Add(Game);
        }

        public void FindUsersForBattle()
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
                string nameMap = MapsForPvp[rnd.Next(0, MapsForPvp.Length)];
                List<MapScene> mapScenes = new(MapSceneFactory.GetAllMapScene());
                MapScene? mapScene = mapScenes.Find(mapScene => mapScene.Map.Code == nameMap);
                if (mapScene == null) throw new Exception("Не найдена нужная карта");
                    game.SetMap(mapScene.Map);

                int IdPlayerInc = 0;
                game.Players.Add(new Player(firstUser, IdPlayerInc++));
                game.Players.Add(new Player(secondUser, IdPlayerInc++));

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

        public void FindUserOneForBattle()
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
                int IdPlayerInc = 0;
                game.Players.Add(new Player(firstUser, IdPlayerInc++));

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
            if (ConfigGameServer.IsTestBattle)
            {
                FindUserOneForBattle();
            }
            else
            {
                FindUsersForBattle();
            }
        }
        public void RemoveUserForSearch(UserAuth user)
        {
            UsersForSearching.Remove(user);
        }

        public void EndBattleByUser(UserAuth user)
        {
            Game? game = Games.Find(game => {
                return game.Players.Find(player => player.UserAuth == user) != null;
            });
            if (game != null)
            {
                game.End();
            }
        }
    }
}
