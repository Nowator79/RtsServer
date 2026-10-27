using Microsoft.Extensions.Logging;
using RtsServer.App.Battle;
using RtsServer.App.NetWork.Tcp;
using RtsServer.App.NetWorkDto;
using RtsServer.App.NetWorkDto.Response;

namespace RtsServer.App.NetWorkHandlers.Game
{
    public class AirUnitReturnToPadProcessor : IProcessor
    {
        public Task Handler(MainResponse response, GameServer context, UserClientTcp clientTcp, CancellationToken cancellationToken)
        {
            var logger = context.GetLogger<AirUnitReturnToPadProcessor>();

            NAirUnitReturnToPadRequest request;
            try
            {
                request = response.GetBody<NAirUnitReturnToPadRequest>();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[AirReturnToPad] Bad body");
                return Task.CompletedTask;
            }

            App.Battle.Game? game = context.BattleManager.Games.Find(
                g => g.Players.Any(p => p.UserAuth.Id == clientTcp.User!.Id));
            if (game == null)
                return Task.CompletedTask;

            Player? player = game.Players.FirstOrDefault(p => p.UserAuth.Id == clientTcp.User!.Id);
            if (player == null)
                return Task.CompletedTask;

            int[] ids = request.UnitIds ?? Array.Empty<int>();
            for (int i = 0; i < ids.Length; i++)
                game.TryOrderAirUnitReturnToPad(ids[i], player.Id);

            return Task.CompletedTask;
        }
    }
}
