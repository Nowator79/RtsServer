using RtsServer.App.NetWorkDto;
using static RtsServer.App.Battle.Player;

namespace RtsServer.App.Adapters
{
    public class PlayerStateAdapter
    {
        public static NPlayerState Get(ResourcesPlayer PlayerState)
        {
            return new NPlayerState(PlayerState.Resources, PlayerState.MaxResources, PlayerState.UseEnergy, PlayerState.LimitEnergy);
        }
    }
}
