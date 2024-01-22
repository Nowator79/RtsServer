using RtsServer.App.NetWork.Tcp;
using RtsServer.App.NetWorkDto.Response;

namespace RtsServer.App.NetWorkHandlers.Game
{
    public class SetTargetUnitsProcessor : IProcessor
    {
        public void Handler(MainResponse response, GameServer context, UserClientTcp clientTcp)
        {
            Buttle.Game? game = context.ButtleManager.Games.Find(
                    gameItem =>
                    {
                        bool isThisUser = false;
                        gameItem.Players.ForEach(player =>
                        {
                            isThisUser = player.UserAuth.Id == clientTcp.User.Id;
                        });
                        return isThisUser;
                    }
                );

            if (game == null) return;
            SetTargetUnits setTargetUnitsReq = response.GetBody<SetTargetUnits>();
            setTargetUnitsReq.UnitsIds.ForEach(unitID =>
            {
                game.Units[unitID].SetTargetPosition(setTargetUnitsReq.Target);
            });
            
        }
    }
}
