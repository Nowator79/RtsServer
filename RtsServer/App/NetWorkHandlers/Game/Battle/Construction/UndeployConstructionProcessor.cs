using Microsoft.Extensions.Logging;
using RtsServer.App.Battle;
using RtsServer.App.NetWork.Tcp;
using RtsServer.App.NetWorkDto;
using RtsServer.App.NetWorkDto.Response;

namespace RtsServer.App.NetWorkHandlers.Game
{
    public class UndeployConstructionProcessor : IProcessor
    {
        public Task Handler(MainResponse response, GameServer context, UserClientTcp clientTcp, CancellationToken cancellationToken)
        {
            var logger = context.GetLogger<UndeployConstructionProcessor>();

            NUndeployConstructionRequest request;
            try
            {
                request = response.GetBody<NUndeployConstructionRequest>();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[Undeploy] Bad body");
                return Task.CompletedTask;
            }

            App.Battle.Game? game = context.BattleManager.Games.Find(
                g => g.Players.Any(p => p.UserAuth.Id == clientTcp.User!.Id));
            if (game == null)
                return Task.CompletedTask;

            Player? player = game.Players.FirstOrDefault(p => p.UserAuth.Id == clientTcp.User!.Id);
            if (player == null)
                return Task.CompletedTask;

            game.TryUndeployConstruction(request.ConstructionId, player.Id);
            return Task.CompletedTask;
        }
    }
}
