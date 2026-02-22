namespace RtsServer.App.NetWorkDto
{
    public struct NGame
    {
        public List<NUser>? Users { get; set; }
        public List<NUnit>? Units { get; set; }
        public List<NConstruction>? Constructions{ get; set; }
        public List<NMissile>? Missiles{ get; set; }
        
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
        public NGame(List<NUnit> units, List<NConstruction> constructions, List<NMissile> missiles)
        {
            Constructions = constructions;
            Units = units;
            Missiles = missiles;
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
