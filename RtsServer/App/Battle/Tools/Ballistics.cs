using RtsServer.App.Battle.Dto;

namespace RtsServer.App.Battle.Tools
{
    /// <summary>
    /// Расчёт баллистической траектории под гравитацией.
    /// Плоскость карты — X/Y, высота — Z.
    /// </summary>
    public static class Ballistics
    {
        /// <summary>
        /// Ниже — выше дуга и больше досягаемость при той же дульной скорости.
        /// При v=11 дальность ~v²/g ≈ 12 (хватает на танковый Range=10).
        /// </summary>
        public const double Gravity = 10.0;

        /// <summary>
        /// Считает начальную скорость снаряда так, чтобы при данной дульной скорости попасть в цель.
        /// </summary>
        public static bool TryComputeLaunchVelocity(
            Vector2Float from,
            double fromHeight,
            Vector2Float to,
            double toHeight,
            double muzzleSpeed,
            out Vector3Float velocity,
            bool preferLowArc = true)
        {
            velocity = default;
            if (muzzleSpeed <= 0.1)
                return false;

            double dx = to.X - from.X;
            double dy = to.Y - from.Y;
            double d = Math.Sqrt(dx * dx + dy * dy);
            double dh = toHeight - fromHeight;

            // Почти вертикальный выстрел
            if (d < 0.05)
            {
                velocity = new Vector3Float(0, 0, Math.Sign(dh == 0 ? 1 : dh) * muzzleSpeed);
                return true;
            }

            double speed = muzzleSpeed;
            if (!TrySolveLaunchAngle(d, dh, speed, Gravity, preferLowArc, out double angleRad)
                && !TrySolveLaunchAngle(d, dh, speed, Gravity, preferLowArc: false, out angleRad))
            {
                // Физически не долетает при заданной скорости — чуть поднимаем v и берём высокую дугу,
                // вместо «прямого» выстрела, который под гравитацией падает коротким.
                speed = Math.Max(speed, ComputeMinMuzzleSpeed(d, dh, Gravity) * 1.02);
                if (!TrySolveLaunchAngle(d, dh, speed, Gravity, preferLowArc: false, out angleRad)
                    && !TrySolveLaunchAngle(d, dh, speed, Gravity, preferLowArc: true, out angleRad))
                {
                    velocity = ComputeDirectVelocity(from, fromHeight, to, toHeight, speed);
                    return true;
                }
            }

            double cos = Math.Cos(angleRad);
            double sin = Math.Sin(angleRad);
            double invD = 1.0 / d;
            double horizSpeed = speed * cos;

            velocity = new Vector3Float(
                dx * invD * horizSpeed,
                dy * invD * horizSpeed,
                speed * sin);
            return true;
        }

        /// <summary>Минимальная дульная скорость, при которой discriminant = 0 (касание цели).</summary>
        public static double ComputeMinMuzzleSpeed(double horizontalDistance, double heightDelta, double gravity)
        {
            double d = Math.Max(horizontalDistance, 1e-6);
            double g = Math.Max(gravity, 1e-6);
            // v² = g * (Δh + sqrt(Δh² + d²))
            double u = g * (heightDelta + Math.Sqrt(heightDelta * heightDelta + d * d));
            return u > 0 ? Math.Sqrt(u) : 0;
        }

        /// <summary>
        /// θ = arctan((v² ± sqrt(v⁴ - g(gd² + 2Δh v²))) / (gd))
        /// </summary>
        public static bool TrySolveLaunchAngle(
            double horizontalDistance,
            double heightDelta,
            double muzzleSpeed,
            double gravity,
            bool preferLowArc,
            out double angleRadians)
        {
            angleRadians = 0;
            double d = horizontalDistance;
            double v = muzzleSpeed;
            double g = gravity;
            if (d <= 1e-6 || v <= 1e-6 || g <= 1e-6)
                return false;

            double v2 = v * v;
            double disc = v2 * v2 - g * (g * d * d + 2.0 * heightDelta * v2);
            if (disc < 0)
                return false;

            double root = Math.Sqrt(disc);
            double numerator = preferLowArc ? (v2 - root) : (v2 + root);
            angleRadians = Math.Atan(numerator / (g * d));
            return true;
        }

        private static Vector3Float ComputeDirectVelocity(
            Vector2Float from,
            double fromHeight,
            Vector2Float to,
            double toHeight,
            double muzzleSpeed)
        {
            Vector3Float delta = new(to.X - from.X, to.Y - from.Y, toHeight - fromHeight);
            double mag = delta.Magnitude();
            if (mag < 1e-6)
                return new Vector3Float(0, 0, muzzleSpeed);
            return delta / mag * muzzleSpeed;
        }
    }
}
