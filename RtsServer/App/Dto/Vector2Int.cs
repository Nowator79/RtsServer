using RtsServer.App.Buttle.Navigator;

namespace RtsServer.App.Buttle.Dto
{
    public struct Vector2Int
    {
        public int X { get; set; }
        public int Y { get; set; }


        public Vector2Int(int x, int y)
        {
            X = x;
            Y = y;
        }

        public Vector2Float GetFloat()
        {
            return new Vector2Float(X, Y);
        }

        public static Vector2Int operator +(Vector2Int counter1, Vector2Int counter2)
        {
            return new Vector2Int(counter1.X + counter2.X, counter1.Y + counter2.Y);
        }

        public static bool operator ==(Vector2Int counter1, Vector2Int counter2)
        {
            return counter1.X == counter2.X && counter1.Y == counter2.Y;
        }

        public static bool operator !=(Vector2Int counter1, Vector2Int counter2)
        {
            return counter1.X != counter2.X || counter1.Y != counter2.Y;
        }
    }
}
