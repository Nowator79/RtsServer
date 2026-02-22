namespace RtsServer.App.FileSystem.Dto
{
    public struct FTile
    {
        public int TypeId { get; set; }
        public int Height { get; set; }

        public FTile(int typeId, int height)
        {
            TypeId = typeId;
            Height = height;
        }
    }
}
