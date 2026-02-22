using Microsoft.Extensions.Logging;
using RtsServer.App.NetWork.Tcp;
using RtsServer.App.NetWorkDto.Response;

namespace RtsServer.App.NetWorkHandlers.Game
{
    public class SetAttackTargetUnitsProcessor : IProcessor
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
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            var _logger = loggerFactory.CreateLogger<SetAttackTargetUnitsProcessor>();

            if (game == null) return;
            SetAttackTargetUnits setTargetUnitsReq = response.GetBody<SetAttackTargetUnits>();
            try
            {

            setTargetUnitsReq.UnitsIds.ForEach(unitID =>
            {
                var unit = game.Units[unitID];
                if (clientTcp.User.Id == unit.OwnerId) {
                    game.Units[unitID].SetAttackTarget(game.Units[setTargetUnitsReq.TargetUnitId]);
                }
            });
            } catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
