namespace RtsServer.App.NetWorkDto.Response
{
    /// <summary>Результат матча для /gameBattle/endGame/.</summary>
    public class EndGameResult
    {
        public string Result { get; set; } = "";
        public int WinnerPlayerId { get; set; }

        public EndGameResult() { }

        public EndGameResult(string result, int winnerPlayerId)
        {
            Result = result;
            WinnerPlayerId = winnerPlayerId;
        }
    }
}
