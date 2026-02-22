using RtsServer.App.Battle.Constructions;
using RtsServer.App.Battle;
using RtsServer.App.Battle.Dto;
using RtsServer.App.Battle.Interfaces;
namespace RtsServer.App.Battle.Constructions
{
    public class HeadquartersConstruction : Construction, IEnergyProvider, IResourceProducer, IResourceStorage
    {
        public const int sizeX = 4;
        public const int sizeY = 4;
        public const int maxHealth = 10000;

        public int LimitResources => 500;

        public HeadquartersConstruction(Vector2Int position, int playerOwner)
            : base(maxHealth, maxHealth, position, new(sizeX, sizeY), "Headquarters", playerOwner) { }

        public int ResourcePerMinute => 50;
        public int EnergyProvided => 50;
        public float Resources { get; set; }

        public void ProduceResources(double deltaTime)
        {
            Resources += (float)(ResourcePerMinute * deltaTime / 60.0);
            if(Resources > LimitResources)
            {
                Resources = LimitResources;
            }
        }

        public override void Update()
        {
            ProduceResources(Game.TimeSystem.GetDelta());
        }
    }
}


