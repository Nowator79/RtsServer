using Microsoft.Extensions.Logging;
using RtsServer.App.Battle;
using RtsServer.App.NetWork.Tcp;
using RtsServer.App.NetWorkDto;
using RtsServer.App.NetWorkDto.Response;

namespace RtsServer.App.NetWorkHandlers.Game
{
    public class BuildConstructionProcessor : IProcessor
    {
        public void Handler(MainResponse response, GameServer context, UserClientTcp clientTcp, CancellationToken cancellationToken)
        {
            var logger = context.GetLogger<BuildConstructionProcessor>();

            NBuildConstructionRequest request;
            try
            {
                request = response.GetBody<NBuildConstructionRequest>();
            }
            catch (System.Exception ex)
            {
                logger.LogError(ex, "[BuildConstruction] Не удалось разобрать тело запроса. Body: {Body}", response?.Body);
                return;
            }

            if (request == null)
            {
                logger.LogWarning("[BuildConstruction] Request пустой");
                return;
            }

            logger.LogDebug("[BuildConstruction] Запрос: Code={Code}, Position=({X},{Y}), User={UserId}",
                request.Code, request.Position.X, request.Position.Y, clientTcp.User?.Id);

            App.Battle.Game? game = context.BattleManager.Games.Find(
                    gameItem =>
                    {
                        foreach (App.Battle.Player player in gameItem.Players)
                        {
                            if (player.UserAuth.Id == clientTcp.User!.Id)
                                return true;
                        }
                        return false;
                    }
                );

            if (game == null)
            {
                logger.LogWarning("[BuildConstruction] Игра не найдена для пользователя {UserId}", clientTcp.User?.Id);
                return;
            }

            Player player = game.Players.First(p => p.UserAuth == clientTcp.User);
            game.TryBuildConstruction(request.Code, request.Position, player.Id);
        }
    }
}
