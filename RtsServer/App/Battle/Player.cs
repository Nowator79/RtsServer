using RtsServer.App.Battle.Interfaces;
using RtsServer.App.DataBase.Dto;

namespace RtsServer.App.Battle
{
    public class Player
    {
        public Game Game { get; }
        public UserAuth UserAuth { get; }
        public int Id { get; private set; }
        public PlayerStateType PlayerState { get; set; }
        public Player(UserAuth UserAuth, int Id, Game Game)
        {
            this.UserAuth = UserAuth;
            this.Id = Id;
            this.Game = Game;
            PlayerState = PlayerStateType.None;
        }
        public ResourcesPlayer GetState()
        {
            ResourcesPlayer State = new();

            float resources = 0;
            float resourcesMax = 0;
            Game.Constructions.Where(c => c.OwnerId == Id).ToList().ForEach(c =>
            {
                if (c is IResourceStorage resourceProducer)
                {
                    resources += resourceProducer.Resources;
                    resourcesMax += resourceProducer.LimitResources;
                }
                if(c is IEnergyProvider energyProvider)
                {
                    State.LimitEnergy = energyProvider.EnergyProvided;
                }
            });

            State.UseEnergy = 0;
            State.Resources = (int)resources;
            State.MaxResources = (int)resourcesMax;
            
            return State;
        }
        public void SetReady()
        {
            PlayerState = PlayerStateType.Ready;
            Game.TryStart();
        }
        public struct ResourcesPlayer
        {
            public int Resources;
            public int MaxResources;
            public int UseEnergy;
            public int LimitEnergy;
        }
        public enum PlayerStateType
        {
            None,
            Ready,
            Playing,
            Loading
        }
    }
}
