using RtsServer.App.Battle.Dto;
using RtsServer.App.Battle.Interfaces;

namespace RtsServer.App.Battle.Units.AttackingPoint
{
    /// <summary>Устарело: используйте AgmAirCannon / AamAirCannon.</summary>
    public class AirCannon : AgmAirCannon
    {
        public AirCannon(Unit currentUnit, float rotation = 0) : base(currentUnit, rotation)
        {
        }
    }
}
