using RtsServer.App.Battle.Dto;

namespace RtsServer.App.NetWorkDto
{
    public class NBuildConstructionRequest
    {
        public string Code { get; set; } = "";
        public Vector2Int Position { get; set; }
    }
}
