namespace RtsServer.App.NetWorkDto
{
    public struct NGame
    {
        public List<NUser>? Users { get; set; }
        public List<NUnit>? Units { get; set; }


        public NGame(List<NUser> users, List<NUnit> units)
        {
            Users = users;
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
