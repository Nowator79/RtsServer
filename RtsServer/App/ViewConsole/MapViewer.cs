using RtsServer.App.Buttle.MapButlle;

namespace RtsServer.App.ViewConsole
{
    public static class MapViewer
    {
        public static void View(Map map)
        {
            for (int x = 0; x < map.Width; x++)
            {
                for (int y = 0; y < map.Length; y++)
                {
                    Console.Write($" {map.Chunks[x * map.Length + y].Height} ");
                }
                Console.WriteLine();
            }
        }
    }
}
