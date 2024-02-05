namespace RtsServer.App.NetWorkDto
{
    public struct NGame
    {
        public List<NUser>? Users { get; set; }
        public List<NUnit>? Units { get; set; }
        public List<NConstruction>? Constructions{ get; set; }

        public NGame(List<NUser> users, List<NUnit> units, List<NConstruction> constructions)
        {
            Users = users;
            Units = units;
            Constructions = constructions;
        }
        public NGame(List<NUser> users, List<NUnit> units)
        {
            Users = users;
            Units = units;
        }
        public NGame(List<NUnit> units, List<NConstruction> constructions)
        {
            Constructions = constructions;
            Units = units;
        }
        public NGame(List<NUser> users)
        {
            Users = users;
        }
        public NGame(List<NUnit> units)
        {
            Units = units;
        }
    }
}
