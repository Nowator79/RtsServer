using RtsServer.App.Buttle.Dto;

namespace RtsServer.App.ViewConsole
{
    public static class HealhtViewer
    {

        public static void View(Health health)
        {
            Console.WriteLine(GetStringView(health));
        }

        public static string GetStringView(Health health)
        {
            return $"{health.Value}";
        }
    }
}
