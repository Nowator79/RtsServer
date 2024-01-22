using RtsServer.App.Buttle.Navigator;

namespace RtsServer.App.Buttle.Dto
{
    public struct Vector2Float
    {
        public double X { get; set; }
        public double Y { get; set; }

        public Vector2Float(double x, double y)
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

        public static Vector2Float operator *(Vector2Float counter1, double x)
        {
            return new Vector2Float(counter1.X * x, counter1.Y * x);
        }

        public static Vector2Float operator /(Vector2Float counter1, double x)
        {
            return new Vector2Float(counter1.X / x, counter1.Y / x);

        }

        public Vector2Float Normalize()
        {
            double locLength = Distance(new Vector2Float(), this);
            if (locLength == 0) return new Vector2Float(0, 0);
            double inv_length = (1 / locLength);
            Vector2Float result;
            result = new Vector2Float(X * inv_length, Y * inv_length);
            return result;
        }

        public static double operator *(Vector2Float counter1, Vector2Float counter2)
        {
            return counter1.X * counter2.X + counter1.Y * counter2.Y;
        }

        public static double AngleByVecotrs(Vector2Float counter1, Vector2Float counter2)
        {
            Vector2Float x = counter1.Normalize();
            Vector2Float y = counter2.Normalize();
            double p = x * y;
            double radian = Math.Acos(p);
            double gradus = (radian * 180) / Math.PI;
            return gradus;
        }
        public static double AngleByVectorsAndRot(Vector2Float G, Vector2Float GV, Vector2Float T)
        {
            Vector2Float GtoH = T - G;
            double angle = AngleByVecotrs(GV, GtoH);
            return angle;
        }

        public static double DistanceSQRT(Vector2Float start, Vector2Float end)
        {
            return (Math.Pow(end.X - start.X, 2) + Math.Pow(end.Y - start.Y, 2));
        }

        public static double Distance(Vector2Float start, Vector2Float end)
        {
            return Math.Sqrt(DistanceSQRT(start, end));
        }

        public static double SideByVector(Vector2Float P1, Vector2Float P2, Vector2Float P3)
        {
            double r = (P3.X - P1.X) * (P2.Y - P1.Y) - (P3.Y - P1.Y) * (P2.X - P1.X);
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

        public static Vector2Float VectorByVectorAndAngle(Vector2Float position, double rotation)
        {

            return VectorByAngle(rotation) + position;
        }

        public static Vector2Float VectorByAngle(double rotation)
        {
            double X = Math.Cos(ToRad(rotation));
            double Y = Math.Sin(ToRad(rotation));
            return new Vector2Float((float)X, (float)Y);
        }

        public static double ToGrad(double rad)
        {
            return (rad * 180) / Math.PI;
        }

        public static double ToRad(double grad)
        {
            return (grad * Math.PI) / 180;
        }
    }

}
