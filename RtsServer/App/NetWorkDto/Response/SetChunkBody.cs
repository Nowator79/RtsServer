using RtsServer.App.Buttle.Dto;

namespace RtsServer.App.NetWorkDto.Response
{
    public class SetChunkBody : IResponse
    {
        public int ChunkId { get; set; }
        public Vector2Int Position { get; set; }
    }
}
