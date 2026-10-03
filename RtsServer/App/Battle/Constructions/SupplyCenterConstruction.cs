using RtsServer.App.Battle.Dto;
using RtsServer.App.Battle.Interfaces;

namespace RtsServer.App.Battle.Constructions
{
    /// <summary>Центр снабжения — склад и добыча ресурсов (как штаб), потребляет энергию.</summary>
    public class SupplyCenterConstruction : Construction, IResourceProducer, IResourceStorage, IEnergyConsumer
    {
        public const int sizeX = 2;
        public const int sizeY = 2;
        public const int maxHealth = 4000;
        public const string Code = "SupplyCenter";
        public const int BuildCost = 120;
        public const float BuildTime = 7f;

        public int LimitResources => 3000;
        public int ResourcePerMinute => 120;
        public int EnergyRequired => IsBuilt ? 25 : 0;
        public float Resources { get; set; }

        public SupplyCenterConstruction(Vector2Int position, int playerOwner)
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
