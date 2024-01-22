using RtsServer.App.Buttle.Navigator;

namespace RtsServer.App.Buttle.Dto
{
    public struct Vector2Float
    {
        public float X { get; set; }
        public float Y { get; set; }

        public Vector2Float(float x, float y)
        {
            X = x;
            Y = y;
        }

        public Vector2Int ToInt()
        {
            return new Vector2Int(Convert.ToInt32(X), Convert.ToInt32(Y));
        }

        public static Vector2Float operator +(Vector2Float counter1, Vector2Float counter2)
        {
            return new Vector2Float(counter1.X + counter2.X, counter1.Y + counter2.Y);
        }
        public static Vector2Float operator -(Vector2Float counter1, Vector2Float counter2)
        {
            return new Vector2Float(counter1.X - counter2.X, counter1.Y - counter2.Y);
        }

        public static bool operator ==(Vector2Float counter1, Vector2Float counter2)
        {
            return counter1.X == counter2.X && counter1.Y == counter2.Y;
        }

        public static bool operator !=(Vector2Float counter1, Vector2Float counter2)
        {
            return counter1.X != counter2.X || counter1.Y != counter2.Y;
        }

        public static Vector2Float operator *(Vector2Float counter1, float x)
        {
            return new Vector2Float(counter1.X * x, counter1.Y * x);
        }

        public static Vector2Float operator /(Vector2Float counter1, float x)
        {
            return new Vector2Float(counter1.X / x, counter1.Y / x);

        }

        public Vector2Float Normalize()
        {
            float locLength = Distance(new Vector2Float(), this);
            if (locLength == 0) return new Vector2Float(0, 0);
            float inv_length = (1 / locLength);
            Vector2Float result;
            result = new Vector2Float(X * inv_length, Y * inv_length);
            return result;
        }

        public static float operator *(Vector2Float counter1, Vector2Float counter2)
        {
            return counter1.X * counter2.X + counter1.Y * counter2.Y;
        }

        public static float AngleByVecotrs(Vector2Float counter1, Vector2Float counter2)
        {
            Vector2Float x = counter1.Normalize();
            Vector2Float y = counter2.Normalize();
            double p = x * y;
            double radian = Math.Acos(p);
            double gradus = (radian * 180) / Math.PI;
            return (float)gradus;
        }
        public static float AngleByVectorsAndRot(Vector2Float G, Vector2Float GV, Vector2Float T)
        {
            Vector2Float GtoH = T - G;
            float angle = AngleByVecotrs(GV, GtoH);
            return angle;
        }

        public static float DistanceSQRT(Vector2Float start, Vector2Float end)
        {
            return (float)(Math.Pow(end.X - start.X, 2) + Math.Pow(end.Y - start.Y, 2));
        }

        public static float Distance(Vector2Float start, Vector2Float end)
        {
            return (float)Math.Sqrt(DistanceSQRT(start, end));
        }

        public static float SideByVector(Vector2Float P1, Vector2Float P2, Vector2Float P3)
        {
            float r = (P3.X - P1.X) * (P2.Y - P1.Y) - (P3.Y - P1.Y) * (P2.X - P1.X);
            if (r > 0)
            {
                return 1;
            }
            else if (r < 0)
            {
                return -1;
            }

            return 0;
        }
    }

}
