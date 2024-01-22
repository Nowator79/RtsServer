using RtsServer.App;

//Vector2Float G = new(457, 610);
//Vector2Float GFacting = new(2775, 1367);
//Vector2Float HPosition = new(457, 889);
//Console.WriteLine(Vector2Float.AngleByVectorsAndRot(G, GFacting, HPosition));
//Console.WriteLine(Vector2Float.SideByVector(G, GFacting, HPosition));

int port = 8080;

GameServer server = new(port);
server.Run();


while (true) Thread.Sleep(1000);

