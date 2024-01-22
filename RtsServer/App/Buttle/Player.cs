using RtsServer.App.DataBase.Dto;

namespace RtsServer.App.Buttle
{
    public class Player
    {
        public UserAuth UserAuth { get; }
        public int Money { get; private set; } 

        public Player(UserAuth UserAuth)
        {
            this.UserAuth = UserAuth;
            Money = 0;
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
