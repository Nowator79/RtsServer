namespace RtsServer.App.NetWorkDto
{
    public struct NUser
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        /// <summary>Токен авторизации. При логине по токену пароль можно не слать.</summary>
        public string Token { get; set; }
        public int Credits { get; set; }
        public int TechCredits { get; set; }
        public int Level { get; set; }
        public int Experience { get; set; }

        public NUser(string userName, string password)
        {
            Id = 0;
            UserName = userName;
            Password = password;
            Token = "";
            Credits = 0;
            TechCredits = 0;
            Level = 0;
            Experience = 0;
        }

        public NUser(int id, string userName, string token, int credits, int techCredits, int level, int experience)
        {
            Id = id;
            UserName = userName;
            Password = "";
            Token = token;
            Credits = credits;
            TechCredits = techCredits;
            Level = level;
            Experience = experience;
        }
    }
}
