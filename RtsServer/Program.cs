using RtsServer.App;

int port = 7912;

GameServer server = new(port);
server.Run();

Console.ReadLine();
server.Cancel();

while (true) Thread.Sleep(1000); 

