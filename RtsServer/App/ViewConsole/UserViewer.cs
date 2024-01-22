using RtsServer.App.Buttle.MapButlle;
using RtsServer.App.DataBase.Dto;
using RtsServer.App.NetWork.Tcp;
using System.Text;

namespace RtsServer.App.ViewConsole
{
    public static class UserViewer
    {
        public static void View(List<UserClientTcp> userClients)
        {
            Console.WriteLine($"{"ID",20} | {"CountRead",10} |  {"CountWrite",10} | {"UserID",10} |  {"UserName",10} |  {"Status",10}");
            Console.WriteLine(new StringBuilder().Insert(0, "=", 90).ToString());

            foreach (UserClientTcp userClient in userClients)
            {
                Console.Write($"{userClient.Id,20} | {userClient.CountRead,10} |  {userClient.CountWrite,10} |");
                if (userClient.User != null)
                {
                    Console.Write($" {userClient.User.Id,10} | {userClient.User.UserName,10}  | {userClient.User.Status.GetStatus(),10}");
                }
                else
                {
                    Console.Write($" {"",10} | {"",10} | {"",10} ");
                }
                Console.WriteLine(" ");
                Console.WriteLine(new StringBuilder().Insert(0, "-", 90).ToString());
            }

        }

        public static void View(List<UserAuth> userClients)
        {
            Console.WriteLine($"{"ID",20} | {"UserName",10} |  {"Password",10}");
            foreach (UserAuth userClient in userClients)
            {
                Console.Write($"{userClient.Id,20} | {userClient.UserName,10} |  {userClient.Password,10} |");
                Console.WriteLine("");
                Console.WriteLine(new StringBuilder().Insert(0, "-", 70).ToString());
            }

            Console.WriteLine(new StringBuilder().Insert(0, "=", 60).ToString());
        }
    }
}
