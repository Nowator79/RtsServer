namespace RtsServer.App.NetWorkDto
{
    public struct NTile
    {
        public int TypeId { get; set; }
        public int Height { get; set; }

        public NTile(int typeId, int height)
        {
            TypeId = typeId;
            Height = height;
        }
    }
}
