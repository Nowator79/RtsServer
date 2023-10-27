using RtsServer.Classes;
using RtsServer.Classes.Buttle;
using RtsServer.Classes.Processor;


int port = 8080;

GameServer server = new(port);
server.Run();






while (true)
{
    Console.ReadLine();
}

