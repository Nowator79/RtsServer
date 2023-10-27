using RtsServer.Classes.Buttle.Dto;

namespace RtsServer.Classes.Buttle.Map.ChunksType
{
    public interface ITypeChunk
    {
        public int Id { get; }

        public string Name { get; }

        public Vector2Int Postion { get; set; }
    }
}
