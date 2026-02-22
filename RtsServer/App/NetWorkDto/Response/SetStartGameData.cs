
using RtsServer.App.Battle.Dto;

namespace RtsServer.App.NetWorkDto.Response
{
    public class SetStartGameData
    {
        public SetStartGameData(string mapCode, int idPlayer, Vector2Int startPosition)
        {
            MapCode = mapCode;
            IdPlayer = idPlayer;
            StartPosition = startPosition;
        }

        public string MapCode { get; set; }
        public int IdPlayer { get; set; }
        public Vector2Int StartPosition { get; set; }
    }
}
