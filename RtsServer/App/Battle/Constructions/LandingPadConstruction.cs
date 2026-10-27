using RtsServer.App.Battle.Dto;
using RtsServer.App.Battle.Interfaces;
using RtsServer.App.Battle.Units;

namespace RtsServer.App.Battle.Constructions
{
    /// <summary>
    /// Посадочная площадка: 4 слота для самолётов. Нужна, чтобы производить/базировать AirUnit.
    /// </summary>
    public class LandingPadConstruction : Construction, IEnergyConsumer
    {
        public const int sizeX = 3;
        public const int sizeY = 3;
        public const int maxHealth = 2800;
        public const string Code = "LandingPad";
        public const int BuildCost = 180;
        public const float BuildTime = 8f;

        public int EnergyRequired => IsBuilt ? 15 : 0;

        public LandingPads LandingPads { get; }

        public LandingPadConstruction(Vector2Int position, int playerOwner)
            : base(maxHealth, maxHealth, position, new(sizeX, sizeY), Code, playerOwner)
        {
            LandingPads = new LandingPads(this);
        }

        public override double GetBodyHeight() => GetGroundElevation() + 0.35;

        /// <summary>Свободные слоты всех площадок игрока.</summary>
        public static int CountFreeSlots(Game? game, int ownerId)
        {
            if (game == null) return 0;
            int free = 0;
            foreach (Construction c in game.Constructions.Concat(game.ConstructionsForAdd))
            {
                if (c is not LandingPadConstruction pad) continue;
                if (pad.IsDestroyed || !pad.IsBuilt || pad.OwnerId != ownerId) continue;
                free += pad.LandingPads.FreeCount;
            }
            return free;
        }

        /// <summary>Сколько AirUnit уже в очередях авиазаводов (ещё без слота).</summary>
        public static int CountQueuedAirUnits(Game? game, int ownerId)
        {
            if (game == null) return 0;
            int n = 0;
            foreach (Construction c in game.Constructions.Concat(game.ConstructionsForAdd))
            {
                if (c is not AirFactoryConstruction factory) continue;
                if (factory.IsDestroyed || factory.OwnerId != ownerId) continue;
                n += factory.CountAirUnitsInProduction;
            }
            return n;
        }

        public static bool OwnerCanAcceptNewAirUnit(Game? game, int ownerId) =>
            CountFreeSlots(game, ownerId) > CountQueuedAirUnits(game, ownerId);

        /// <summary>Ближайшая площадка владельца со свободным слотом.</summary>
        public static LandingPadConstruction? FindNearestWithFreeSlot(
            Game? game, int ownerId, Vector2Float near)
        {
            if (game == null) return null;

            LandingPadConstruction? best = null;
            double bestDist = double.MaxValue;

            foreach (Construction c in game.Constructions.Concat(game.ConstructionsForAdd))
            {
                if (c is not LandingPadConstruction pad) continue;
                if (pad.IsDestroyed || !pad.IsBuilt || !pad.IsPowered || pad.OwnerId != ownerId)
                    continue;
                if (pad.LandingPads.FreeCount <= 0)
                    continue;

                double dist = Vector2Float.Distance(near, pad.AimPosition);
                if (dist >= bestDist) continue;
                bestDist = dist;
                best = pad;
            }

            return best;
        }
    }
}
