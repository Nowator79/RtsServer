
using RtsServer.App.Buttle.Dto;

namespace RtsServer.App.NetWorkDto.Response
{
    public class SetStartGameData
    {
        public SetStartGameData(string mapCode, Vector2Int startPosition)
        {
            MapCode = mapCode;
            StartPosition = startPosition;
        }

        public string MapCode { get; set; }
        public Vector2Int StartPosition { get; set; }
    }
}
