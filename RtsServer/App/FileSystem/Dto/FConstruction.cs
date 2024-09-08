using RtsServer.App.Battle.Dto;

namespace RtsServer.App.FileSystem.Dto
{
    public struct FConstruction
    {
        public int PlayerOwnerNum { get; set; }
        public string Code { get; set; }
        public Vector2Int Position { get; set; }
    }
}
