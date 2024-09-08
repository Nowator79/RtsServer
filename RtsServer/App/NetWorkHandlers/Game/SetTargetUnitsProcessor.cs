using RtsServer.App.NetWork.Tcp;
using RtsServer.App.NetWorkDto.Response;

namespace RtsServer.App.NetWorkHandlers.Game
{
    public class SetTargetUnitsProcessor : IProcessor
    {
        public void Handler(MainResponse response, GameServer context, UserClientTcp clientTcp)
        {
            App.Battle.Game? game = context.BattleManager.Games.Find(
                    gameItem =>
                    {
                        bool isThisUser = false;

                        foreach (App.Battle.Player player in gameItem.Players)
                        {
                            isThisUser = player.UserAuth.Id == clientTcp.User.Id;
                            if (isThisUser) break;

                        }
         
                        return isThisUser;
                    }
                );

            if (game == null) return;
            SetTargetUnits setTargetUnitsReq = response.GetBody<SetTargetUnits>();
            setTargetUnitsReq.UnitsIds.ForEach(unitID =>
            {
                var unit = game.Units[unitID];
                if (clientTcp.User.Id == unit.PlayerOwner) {
                    game.Units[unitID].SetTargetPosition(setTargetUnitsReq.Target);
                }
            });
            
        }
    }
}
