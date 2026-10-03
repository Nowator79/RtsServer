using Microsoft.Extensions.Logging;
using RtsServer.App.Battle;
using RtsServer.App.NetWork.Tcp;
using RtsServer.App.NetWorkDto;
using RtsServer.App.NetWorkDto.Response;

namespace RtsServer.App.NetWorkHandlers.Game
{
    public class ProduceUnitProcessor : IProcessor
    {
        public Task Handler(MainResponse response, GameServer context, UserClientTcp clientTcp, CancellationToken cancellationToken)
        {
            var logger = context.GetLogger<ProduceUnitProcessor>();

            NProduceUnitRequest request;
            try
            {
                request = response.GetBody<NProduceUnitRequest>();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[ProduceUnit] Не удалось разобрать тело. Body: {Body}", response?.Body);
                return Task.CompletedTask;
            }

            if (request == null || string.IsNullOrWhiteSpace(request.UnitCode))
            {
                logger.LogWarning("[ProduceUnit] Пустой запрос");
                return Task.CompletedTask;
            }

            App.Battle.Game? game = context.BattleManager.Games.Find(
                gameItem => gameItem.Players.Any(p => p.UserAuth.Id == clientTcp.User!.Id));

            if (game == null)
            {
                logger.LogWarning("[ProduceUnit] Игра не найдена для {UserId}", clientTcp.User?.Id);
                return Task.CompletedTask;
            }

            Player? player = game.Players.FirstOrDefault(p => p.UserAuth.Id == clientTcp.User!.Id);
            if (player == null)
            {
                logger.LogWarning("[ProduceUnit] Игрок не найден для {UserId}", clientTcp.User?.Id);
                return Task.CompletedTask;
            }

            game.TryProduceUnit(request.FactoryId, request.UnitCode, player.Id);
            return Task.CompletedTask;
        }
    }
}
