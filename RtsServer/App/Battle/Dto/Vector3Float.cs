namespace RtsServer.App.Battle.Dto
{
    /// <summary>3D-вектор: X/Y — плоскость карты, Z — высота.</summary>
    public struct Vector3Float
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public Vector3Float(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Vector2Float ToMap() => new((float)X, (float)Y);

        public double Magnitude() => Math.Sqrt(X * X + Y * Y + Z * Z);

        public Vector3Float Normalized()
        {
            double mag = Magnitude();
            return mag > 1e-10 ? this / mag : new Vector3Float(0, 0, 0);
        }

        public static Vector3Float operator +(Vector3Float a, Vector3Float b) =>
            new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

        public static Vector3Float operator -(Vector3Float a, Vector3Float b) =>
            new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

        public static Vector3Float operator *(Vector3Float a, double d) =>
            new(a.X * d, a.Y * d, a.Z * d);

        public static Vector3Float operator /(Vector3Float a, double d) =>
            new(a.X / d, a.Y / d, a.Z / d);
    }
}
