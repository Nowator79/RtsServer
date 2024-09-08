using RtsServer.App.DataBase.Dto;

namespace RtsServer.App.Battle
{
    public class Player
    {
        public UserAuth UserAuth { get; }
        public int Money { get; private set; } 
        public int IdBattlePlayer { get; private set; }

        public Player(UserAuth UserAuth, int idBattlePlayer)
        {
            this.UserAuth = UserAuth;
            Money = 0;
            IdBattlePlayer = idBattlePlayer;
        }

        public void UpMoney(int money)
        {
            Money += money;
        }

        public void ВebitingMoney(int money)
        {
            Money -= money;
        }

        public int GetPowerEnegry()
        {
            throw new NotImplementedException();
        }
    }
}
