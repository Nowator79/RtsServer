using RtsServer.App.Battle.Dto;

namespace RtsServer.App.NetWorkDto.Response
{
    public class SetChunkBody
    {
        public int ChunkId { get; set; }
        public Vector2Int Position { get; set; }
    }
}
