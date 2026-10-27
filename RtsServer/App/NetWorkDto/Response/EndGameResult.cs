namespace RtsServer.App.NetWorkDto.Response
{
    /// <summary>Результат матча для /gameBattle/endGame/.</summary>
    public class EndGameResult
    {
        public string Result { get; set; } = "";
        public int WinnerPlayerId { get; set; }
        /// <summary>Длительность матча в секундах (на момент конца).</summary>
        public float MatchDurationSeconds { get; set; }
        /// <summary>Сколько опыта начислено за этот матч.</summary>
        public int XpGained { get; set; }
        /// <summary>Сколько серебра (Credits) начислено за этот матч.</summary>
        public int CreditsGained { get; set; }

        public EndGameResult() { }

        public EndGameResult(
            string result,
            int winnerPlayerId,
            float matchDurationSeconds = 0f,
            int xpGained = 0,
            int creditsGained = 0)
        {
            Result = result;
            WinnerPlayerId = winnerPlayerId;
            MatchDurationSeconds = matchDurationSeconds;
            XpGained = xpGained;
            CreditsGained = creditsGained;
        }
    }
}
