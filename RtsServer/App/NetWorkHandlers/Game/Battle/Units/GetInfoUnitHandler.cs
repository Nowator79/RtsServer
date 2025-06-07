using RtsServer.App.Adapters;
using RtsServer.App.NetWork.Tcp;
using RtsServer.App.NetWorkDto.Response;
using RtsServer.App.NetWorkResponseSender;

namespace RtsServer.App.NetWorkHandlers.Game
{
    public class GetInfoUnitHandler : IProcessor
    {
        public void Handler(MainResponse response, GameServer context, UserClientTcp clientTcp, CancellationToken cancellationToken)
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
            GetInfoUnit getInfoUnitReq = response.GetBody<GetInfoUnit>();
          
            var unit = game.Units[getInfoUnitReq.UnitId];

            (new UnitInfoSender(clientTcp)).SetDate(UnitAdapter.Get(unit)).SendMessage();
            //if (clientTcp.User.Id == unit.PlayerOwner) {
            //    game.Units[unitID].SetTargetPosition(setTargetUnitsReq.Target);
            //}

        }
    }
}
