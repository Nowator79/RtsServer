using System;
using System.Runtime.CompilerServices;

namespace RtsServer.App.Battle.Dto
{
    public struct Vector2Float : IEquatable<Vector2Float>
    {
        public double X { get; set; }
        public double Y { get; set; }

        public static Vector2Float Zero { get; } = new Vector2Float(0, 0);
        public static Vector2Float One { get; } = new Vector2Float(1, 1);
        public static Vector2Float UnitX { get; } = new Vector2Float(1, 0);
        public static Vector2Float UnitY { get; } = new Vector2Float(0, 1);

        private const double Deg2Rad = Math.PI / 180;
        private const double Rad2Deg = 180 / Math.PI;
        private const double Epsilon = 1e-10;

        public Vector2Float(double x, double y)
        {
            X = x;
            Y = y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2Int ToInt() => new Vector2Int((int)Math.Round(X), (int)Math.Round(Y));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double Magnitude() => Math.Sqrt(X * X + Y * Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double SqrMagnitude() => X * X + Y * Y;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2Float Normalized()
        {
            double mag = Magnitude();
            return mag > Epsilon ? this / mag : Zero;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2Float Lerp(Vector2Float a, Vector2Float b, double t)
        {
            t = Math.Clamp(t, 0, 1);
            return new Vector2Float(
                a.X + (b.X - a.X) * t,
                a.Y + (b.Y - a.Y) * t
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Dot(Vector2Float a, Vector2Float b) => a.X * b.X + a.Y * b.Y;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Cross(Vector2Float a, Vector2Float b) => a.X * b.Y - a.Y * b.X;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Distance(Vector2Float a, Vector2Float b)
        {
            double dx = a.X - b.X;
            double dy = a.Y - b.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Angle(Vector2Float from, Vector2Float to)
        {
            double denominator = Math.Sqrt(from.SqrMagnitude() * to.SqrMagnitude());
            if (denominator < Epsilon)
                return 0;

            double dot = Math.Clamp(Dot(from, to) / denominator, -1, 1);
            return Math.Acos(dot) * Rad2Deg;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2Float Rotate(Vector2Float v, double degrees)
        {
            double radians = degrees * Deg2Rad;
            double cos = Math.Cos(radians);
            double sin = Math.Sin(radians);
            return new Vector2Float(
                v.X * cos - v.Y * sin,
                v.X * sin + v.Y * cos
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2Float operator +(Vector2Float a, Vector2Float b) =>
            new Vector2Float(a.X + b.X, a.Y + b.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2Float operator -(Vector2Float a, Vector2Float b) =>
            new Vector2Float(a.X - b.X, a.Y - b.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2Float operator *(Vector2Float a, double d) =>
            new Vector2Float(a.X * d, a.Y * d);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2Float operator *(double d, Vector2Float a) =>
            new Vector2Float(a.X * d, a.Y * d);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2Float operator /(Vector2Float a, double d) =>
            new Vector2Float(a.X / d, a.Y / d);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Vector2Float left, Vector2Float right) =>
            left.Equals(right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Vector2Float left, Vector2Float right) =>
            !left.Equals(right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Vector2Float other) =>
            Math.Abs(X - other.X) < Epsilon &&
            Math.Abs(Y - other.Y) < Epsilon;

        public override bool Equals(object obj) => obj is Vector2Float other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(X, Y);

        public override string ToString() => $"({X:F2}, {Y:F2})";

        // Методы для обратной совместимости
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Vector2Float Normalize() => Normalized();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double AngleByVecotrs(Vector2Float a, Vector2Float b) => Angle(a, b);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double AngleByVectorsAndRot(Vector2Float g, Vector2Float gv, Vector2Float t) =>
            Angle(gv, t - g);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double DistanceSQRT(Vector2Float a, Vector2Float b)
        {
            double dx = a.X - b.X;
            double dy = a.Y - b.Y;
            return dx * dx + dy * dy;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double SideByVector(Vector2Float p1, Vector2Float p2, Vector2Float p3) =>
            Math.Sign(Cross(p2 - p1, p3 - p1));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2Float VectorByVectorAndAngle(Vector2Float pos, double angle) =>
            Rotate(UnitX, angle) + pos;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2Float VectorByAngle(double angle) =>
            Rotate(UnitX, angle);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double ToGrad(double rad) => rad * Rad2Deg;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ReachDistance(Vector2Float curPos, Vector2Float target, float distance) => DistanceSQRT(curPos, target) < distance * distance;

        public static explicit operator Vector2Int(Vector2Float v)
        {
            return new Vector2Int((int)v.X, (int)v.Y);
        }
    }
}