using RtsServer.App.Battle.Dto;
using RtsServer.App.Battle.Interfaces;

namespace RtsServer.App.Battle.Constructions
{
    public class HeadquartersConstruction : Construction, IEnergyProvider, IResourceProducer, IResourceStorage, IEnergyConsumer
    {
        public const int sizeX = 4;
        public const int sizeY = 4;
        public const int maxHealth = 10000;
        public const string Code = "Headquarters";
        public const int BuildCost = 150;
        /// <summary>Время возведения в секундах.</summary>
        public const float BuildTime = 8f;

        public int LimitResources => 3000;

        public HeadquartersConstruction(Vector2Int position, int playerOwner)
            : base(maxHealth, maxHealth, position, new(sizeX, sizeY), Code, playerOwner) { }

        public bool TrySpend(float amount)
        {
            if (!IsBuilt || Resources < amount) return false;
            Resources -= amount;
            return true;
        }

        public void AddResources(float amount)
        {
            if (!IsBuilt) return;
            Resources = Math.Min(Resources + amount, LimitResources);
        }

        public int ResourcePerMinute => 50;
        public int EnergyProvided => IsBuilt ? 50 : 0;
        public int EnergyRequired => IsBuilt ? 10 : 0;
        public float Resources { get; set; }

        public void ProduceResources(double deltaTime)
        {
            if (!IsBuilt || !IsPowered) return;
            Resources += (float)(ResourcePerMinute * deltaTime / 60.0);
            if (Resources > LimitResources)
                Resources = LimitResources;
        }

        public override void Update()
        {
            base.Update();
            if (Game == null) return;
            ProduceResources(Game.TimeSystem.GetDelta());
        }
    }
}
