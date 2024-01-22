namespace RtsServer.App.Dto
{
    public struct StatusUser
    {
        public const string IsGameStatus = "isGame";
        public const string IsSearchStatus = "isSearch";
        public const string IsPassiveStatus = "isPassive";
        private string Status { get; set; }

        public StatusUser()
        {
            Status = IsPassiveStatus;
        }

        public void SetInGame()
        {
            Status = IsGameStatus;
        }
        public void SetInSearch()
        {
            Status = IsSearchStatus;
        }
        public void SetInPassive()
        {
            Status = IsPassiveStatus;
        }
        public string GetStatus()
        {
            return Status;
        }
    }
}
