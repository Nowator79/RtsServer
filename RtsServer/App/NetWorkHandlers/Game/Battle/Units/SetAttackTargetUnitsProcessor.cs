using Microsoft.Extensions.Logging;
using RtsServer.App.Battle.Constructions;
using RtsServer.App.Battle.Interfaces;
using RtsServer.App.Battle.Units;
using RtsServer.App.NetWork.Tcp;
using RtsServer.App.NetWorkDto.Response;

namespace RtsServer.App.NetWorkHandlers.Game
{
    public class SetAttackTargetUnitsProcessor : IProcessor
    {
        /// <summary>TypeTarget: 1 = юнит, 3 = постройка.</summary>
        public const int TypeUnit = 1;
        public const int TypeConstruction = 3;

        public Task Handler(MainResponse response, GameServer context, UserClientTcp clientTcp, CancellationToken cancellationToken)
        {
            var logger = context.GetLogger<SetAttackTargetUnitsProcessor>();

            App.Battle.Game? game = context.BattleManager.Games.FirstOrDefault(
                g => g.Players.Any(p => p.UserAuth.Id == clientTcp.User!.Id));

            if (game == null) return Task.CompletedTask;

            SetAttackTargetUnits req;
            try
            {
                req = response.GetBody<SetAttackTargetUnits>();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Не удалось разобрать SetAttackTargetUnits");
                return Task.CompletedTask;
            }

            App.Battle.Player? player = game.Players.FirstOrDefault(p => p.UserAuth.Id == clientTcp.User!.Id);
            if (player == null) return Task.CompletedTask;

            IAttackTarget? target = ResolveTarget(game, req.TargetUnitId, req.TypeTarget);
            if (target == null || target.IsDestroyed)
                return Task.CompletedTask;

            // Свои здания/юниты не атакуем.
            if (target.OwnerId == player.Id)
                return Task.CompletedTask;

            foreach (int unitId in req.UnitsIds ?? Enumerable.Empty<int>())
            {
                Unit? unit = game.Units.FirstOrDefault(u => u.Id == unitId);
                if (unit == null || unit.OwnerId != player.Id || unit.IsDestroyed)
                    continue;

                unit.SetAttackTarget(target);
            }

            return Task.CompletedTask;
        }

        private static IAttackTarget? ResolveTarget(App.Battle.Game game, int targetId, int typeTarget)
        {
            if (typeTarget == TypeConstruction)
                return game.Constructions.FirstOrDefault(c => c.Id == targetId && !c.IsDestroyed);

            return game.Units.FirstOrDefault(u => u.Id == targetId && !u.IsDestroyed);
        }
    }
}
