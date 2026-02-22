using RtsServer.App.Battle.Units;
using RtsServer.App.NetWorkDto;

namespace RtsServer.App.Adapters
{
    public class MissileAdapter
    {
        public static NMissile Get(Missile missile)
        {
            return new NMissile(missile.Id, missile.Code, missile.Position);
        }
    }
}



