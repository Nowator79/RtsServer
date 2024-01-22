namespace RtsServer.App.FileSystem.Dto
{
    public struct FMap
    {
        public List<FChank> Chunks { get; set; }
        public int Width { get; set; }
        public int Length { get; set; }
        public string Name { get; set; }

    }
}
