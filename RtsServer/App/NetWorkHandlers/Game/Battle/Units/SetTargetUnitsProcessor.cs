using RtsServer.App.Battle.Units;
using RtsServer.App.NetWork.Tcp;
using RtsServer.App.NetWorkDto.Response;

namespace RtsServer.App.NetWorkHandlers.Game
{
    public class SetTargetUnitsProcessor : IProcessor
    {
        public void Handler(MainResponse response, GameServer context, UserClientTcp clientTcp, CancellationToken cancellationToken)
        {
            // Найдём игру, в которой участвует текущий пользователь
            App.Battle.Game? game = context.BattleManager.Games.FirstOrDefault(
                g => g.Players.Any(p => p.UserAuth.Id == clientTcp.User.Id)
            );

            if (game == null)
                return;

            // Получим объект запроса
            SetTargetUnits setTargetUnitsReq = response.GetBody<SetTargetUnits>();

            // Найдём игрока один раз, чтобы не искать его на каждой итерации
            App.Battle.Player? player = game.Players.FirstOrDefault(p => p.UserAuth.Id == clientTcp.User.Id);
            if (player == null)
                return;

            int idPlayer = player.Id;

            // Проходим по каждому ID юнита из запроса
            foreach (int unitID in setTargetUnitsReq.UnitsIds)
            {
                // Проверка на существование юнита
                var unit = game.Units.FirstOrDefault(u => u.Id == unitID);
                if (unit == null)
                    continue;

                // Проверяем, принадлежит ли юнит игроку
                if (unit.OwnerId != idPlayer)
                    continue;

                // Устанавливаем цель
                unit.SetTargetPosition(setTargetUnitsReq.Target);
            }
        }
    }
}
