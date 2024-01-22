namespace RtsServer.App.FileSystem.Dto
{
    public struct FChank
    {
        public int TypeId { get; set; }
        public int Height { get; set; }

        public FChank(int typeId, int height)
        {
            TypeId = typeId;
            Height = height;
        }
    }
}
