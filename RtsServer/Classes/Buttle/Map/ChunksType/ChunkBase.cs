using RtsServer.Classes.Buttle.Dto;

namespace RtsServer.Classes.Buttle.Map.ChunksType
{
    public class ChunkBase : ITypeChunk
    {
        public int Id { get; set; } = 1;
        public static string NameChunk = "Ground";
        public virtual string Name { get; set; } = NameChunk;

        public Vector2Int Postion { get; set; }
    }
}
