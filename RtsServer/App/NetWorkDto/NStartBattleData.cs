namespace RtsServer.App.NetWorkDto
{
    public struct NStartBattleData
    {
        public string Type { get; set; }

        public NStartBattleData(string type)
        {
            Type = type;
        }
    }
}
