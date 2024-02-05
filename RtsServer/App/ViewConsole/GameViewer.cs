
using RtsServer.App.Buttle;
using System.Text;

namespace RtsServer.App.ViewConsole
{
    public static class GameViewer
    {
        public static void View(Game game)
        {
            Console.WriteLine($"Game ID: {game.Id}, TimeCreated: {new DateTime(game.CreateDateTime).ToLongTimeString()} TimeButtle: {new DateTime(DateTime.Now.Ticks - game.CreateDateTime).ToLongTimeString()}");
            Console.WriteLine($"{"Id",20} | {"XmlCode",20} | {"Health",10} |  {"Position",10} |");

            foreach (Buttle.Units.Unit unit in game.Units)
            {
                Console.WriteLine(new StringBuilder().Insert(0, "-", 72).ToString());
                Console.Write($"{unit.Id,20} | {unit.Code,20} | {HealhtViewer.GetStringView(unit.Health),10} |  {VectorViewer.GetStringView(unit.Position),10} |");
                Console.WriteLine();
            }
            Console.WriteLine(new StringBuilder().Insert(0, "=", 72).ToString());

        }

        public static void ViewFullInfo(Game game)
        {
            if (ConfigGameServer.IsDebugGameUsersInfoUpdate)
            {
                Console.WriteLine($"Game ID: {game.Id}, TimeCreated: {new DateTime(game.CreateDateTime).ToLongTimeString()} TimeButtle: {new DateTime(DateTime.Now.Ticks - game.CreateDateTime).ToLongTimeString()}");
                Console.WriteLine($"{"Id",20} | {"XmlCode",20} | {"Health",10} |  {"Position",10} |");

                foreach (Buttle.Units.Unit unit in game.Units)
                {
                    Console.WriteLine(new StringBuilder().Insert(0, "-", 72).ToString());
                    Console.Write($"{unit.Id,20} | {unit.Code,20} | {HealhtViewer.GetStringView(unit.Health),10} |  {VectorViewer.GetStringView(unit.Position),10} |");
                    Console.WriteLine();
                }
                Console.WriteLine(new StringBuilder().Insert(0, "=", 72).ToString());
            }

            char[,] viewConsoleArray = new char[game.Map.Width, game.Map.Length];
            for (int x = 0; x < game.Map.Width; x++)
            {
                for (int y = 0; y < game.Map.Length; y++)
                {
                    viewConsoleArray[x, y] = game.Map.Chunks[x * game.Map.Length + y].Height == 1 ? '=' : '~';
                }
            }

            game.Units.ForEach(unit =>
            {
                foreach (Buttle.Dto.Vector2Int point in unit.PathRout)
                {
                    viewConsoleArray[point.X, point.Y] = '?';
                }
                viewConsoleArray[Convert.ToInt16(unit.Position.X), Convert.ToInt16(unit.Position.Y)] = 'u';
            });

            for (int x = 0; x < game.Map.Width; x++)
            {
                for (int y = 0; y < game.Map.Length; y++)
                {
                    Console.Write($"{viewConsoleArray[x, y], 2}");
                }
                Console.WriteLine();
            }
        }
    }
}
