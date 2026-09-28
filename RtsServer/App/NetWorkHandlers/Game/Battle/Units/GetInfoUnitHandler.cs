using RtsServer.App.Adapters;
using RtsServer.App.NetWork.Tcp;
using RtsServer.App.NetWorkDto.Response;
using RtsServer.App.NetWorkResponseSender;

namespace RtsServer.App.NetWorkHandlers.Game
{
    public class GetInfoUnitHandler : IProcessor
    {
        public async Task Handler(MainResponse response, GameServer context, UserClientTcp clientTcp, CancellationToken cancellationToken)
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

            if (getInfoUnitReq.UnitId < 0 || getInfoUnitReq.UnitId >= game.Units.Count)
                return;

            var unit = game.Units[getInfoUnitReq.UnitId];

            await new UnitInfoSender(clientTcp)
                .SetDate(UnitAdapter.Get(unit))
                .SendAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
