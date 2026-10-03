using RtsServer.App.Battle.Navigator;
using RtsServer.App.Battle.Units;
using RtsServer.App.NetWork.Tcp;
using RtsServer.App.NetWorkDto.Response;

namespace RtsServer.App.NetWorkHandlers.Game
{
    public class SetTargetUnitsProcessor : IProcessor
    {
        public Task Handler(MainResponse response, GameServer context, UserClientTcp clientTcp, CancellationToken cancellationToken)
        {
            App.Battle.Game? game = context.BattleManager.Games.FirstOrDefault(
                g => g.Players.Any(p => p.UserAuth.Id == clientTcp.User!.Id)
            );

            if (game?.Map == null)
                return Task.CompletedTask;

            SetTargetUnits setTargetUnitsReq = response.GetBody<SetTargetUnits>();

            App.Battle.Player? player = game.Players.FirstOrDefault(p => p.UserAuth.Id == clientTcp.User!.Id);
            if (player == null)
                return Task.CompletedTask;

            int idPlayer = player.Id;
            bool attackMove = setTargetUnitsReq.TypeTarget == 2;

            List<Unit> ordered = new();
            foreach (int unitID in setTargetUnitsReq.UnitsIds ?? Enumerable.Empty<int>())
            {
                Unit? unit = game.Units.FirstOrDefault(u => u.Id == unitID);
                if (unit == null || unit.IsDestroyed || unit.OwnerId != idPlayer)
                    continue;
                ordered.Add(unit);
            }

            if (ordered.Count == 0)
                return Task.CompletedTask;

            GroupMoveAssigner.IssueMove(ordered, setTargetUnitsReq.Target, attackMove, game.Map);
            return Task.CompletedTask;
        }
    }
}
