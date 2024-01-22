using RtsServer.App;
using RtsServer.App.Buttle.Dto;

//Vector2Float G = new(1, 3);
//Vector2Float GFacting = new(1, 1);
//Vector2Float HPosition = new(3, 2);
//Console.WriteLine(Vector2Float.AngleByVectorsAndRot(G, GFacting, HPosition));
//Console.WriteLine(Vector2Float.SideByVector(G, GFacting, HPosition));
//return;
int port = 8080;

GameServer server = new(port);
server.Run();


while (true) Thread.Sleep(1000);

