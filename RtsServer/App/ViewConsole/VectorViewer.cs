using RtsServer.App.Battle.Dto;

namespace RtsServer.App.ViewConsole
{
    public static class VectorViewer
    {

        public static void View(Vector2Float vector)
        {
            Console.WriteLine(GetStringView(vector));
        }

        public static string GetStringView(Vector2Float vector)
        {
            return $"{vector.X}: {vector.Y}";
        }
    }
}
