namespace RtsServer.App.NetWorkDto
{
    public struct NPlayerState
    {
        public NPlayerState(int resources, int maxResources, int useEnergy, int limitEnergy)
        {
            Resources = resources;
            MaxResources = maxResources;
            LimitEnergy = limitEnergy;
            UseEnergy = useEnergy;
        }

        public int Resources { get; set; }
        public int MaxResources { get; set; }
        public int UseEnergy { get; set; }
        public int LimitEnergy { get; set; }

    }
}
