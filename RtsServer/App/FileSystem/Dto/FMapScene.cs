namespace RtsServer.App.FileSystem.Dto
{
    public struct FMapScene
    {
        public string MapCode { get; set; }
        public FConstruction[] Constuctins { get; set; }
        public FUnit[] Units { get; set; }
    }
}
