namespace RtsServer.Classes.Buttle.Dto
{
    public struct Vector2Int
    {
        public int X { get; set; }
        public int Y { get; set; }

        public Vector2Int()
        {
            X = 0;
            Y = 0;
        }

        public Vector2Int(int x, int y)
        {
            X = x;
            Y = y;
        }
    }
}
