using RtsServer.App.Battle.Units;
using RtsServer.App.NetWorkDto;

namespace RtsServer.App.Adapters
{
    public class MissileAdapter
    {
        public static NMissile Get(Missile missile)
        {
            double horizSpeed = Math.Sqrt(missile.Velocity.X * missile.Velocity.X + missile.Velocity.Y * missile.Velocity.Y);
            float yaw = (float)(Math.Atan2(missile.Velocity.X, missile.Velocity.Y) * (180.0 / Math.PI));
            float pitch = horizSpeed > 1e-4
                ? (float)(Math.Atan2(missile.Velocity.Z, horizSpeed) * (180.0 / Math.PI))
                : (missile.Velocity.Z >= 0 ? 90f : -90f);

            // В клиенте yaw вокруг Y: 0 смотрит вдоль +Z (карта Y). Согласуем с юнитами.
            return new NMissile(
                missile.Id,
                missile.Code,
                missile.Position,
                (float)missile.Height,
                yaw,
                pitch);
        }
    }
}
