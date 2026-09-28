using RtsServer.App.NetWork.Tcp;
using RtsServer.App.NetWorkDto.Response;

namespace RtsServer.App.NetWorkHandlers.Game
{
    public class SetTargetUnitsProcessor : IProcessor
    {
        public Task Handler(MainResponse response, GameServer context, UserClientTcp clientTcp, CancellationToken cancellationToken)
        {
            App.Battle.Game? game = context.BattleManager.Games.FirstOrDefault(
                g => g.Players.Any(p => p.UserAuth.Id == clientTcp.User.Id)
            );

            if (game == null)
                return Task.CompletedTask;

            SetTargetUnits setTargetUnitsReq = response.GetBody<SetTargetUnits>();

            App.Battle.Player? player = game.Players.FirstOrDefault(p => p.UserAuth.Id == clientTcp.User.Id);
            if (player == null)
                return Task.CompletedTask;

            int idPlayer = player.Id;

            foreach (int unitID in setTargetUnitsReq.UnitsIds)
            {
                var unit = game.Units.FirstOrDefault(u => u.Id == unitID);
                if (unit == null)
                    continue;

                if (unit.OwnerId != idPlayer)
                    continue;

                unit.SetTargetPosition(setTargetUnitsReq.Target);
            }

            return Task.CompletedTask;
        }
    }
}
