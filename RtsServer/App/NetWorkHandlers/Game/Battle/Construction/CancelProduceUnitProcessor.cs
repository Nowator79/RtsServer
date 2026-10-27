using Microsoft.Extensions.Logging;
using RtsServer.App.Battle;
using RtsServer.App.NetWork.Tcp;
using RtsServer.App.NetWorkDto;
using RtsServer.App.NetWorkDto.Response;

namespace RtsServer.App.NetWorkHandlers.Game
{
    public class CancelProduceUnitProcessor : IProcessor
    {
        public Task Handler(MainResponse response, GameServer context, UserClientTcp clientTcp, CancellationToken cancellationToken)
        {
            var logger = context.GetLogger<CancelProduceUnitProcessor>();

            NCancelProduceUnitRequest request;
            try
            {
                request = response.GetBody<NCancelProduceUnitRequest>();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[CancelProduce] Не удалось разобрать тело. Body: {Body}", response?.Body);
                return Task.CompletedTask;
            }

            if (request == null || string.IsNullOrWhiteSpace(request.UnitCode))
            {
                logger.LogWarning("[CancelProduce] Пустой запрос");
                return Task.CompletedTask;
            }

            App.Battle.Game? game = context.BattleManager.Games.Find(
                gameItem => gameItem.Players.Any(p => p.UserAuth.Id == clientTcp.User!.Id));

            if (game == null)
            {
                logger.LogWarning("[CancelProduce] Игра не найдена для {UserId}", clientTcp.User?.Id);
                return Task.CompletedTask;
            }

            Player? player = game.Players.FirstOrDefault(p => p.UserAuth.Id == clientTcp.User!.Id);
            if (player == null)
            {
                logger.LogWarning("[CancelProduce] Игрок не найден для {UserId}", clientTcp.User?.Id);
                return Task.CompletedTask;
            }

            game.TryCancelProduceUnit(request.FactoryId, request.UnitCode, player.Id);
            return Task.CompletedTask;
        }
    }
}
