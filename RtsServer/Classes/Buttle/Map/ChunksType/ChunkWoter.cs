using RtsServer.Classes.Buttle.Dto;

namespace RtsServer.Classes.Buttle.Map.ChunksType
{
    public class ChunkWoter : ChunkBase
    {
        public int Id { get; set; } = 1;

        public static string NameChunk = "Woter";

        public override string Name { get; set; } = NameChunk;

        public Vector2Int Postion { get; set; }
    }
}
