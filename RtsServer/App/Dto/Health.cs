using RtsServer.App.Battle.Units;

namespace RtsServer.App.Battle.Dto
{
    public class Health
    {
        public float Value { get; private set; }
        private int Max;
        private BattleEntity _entity;

        public Health(int value, int max, BattleEntity entity)
        {
            Value = value;
            Max = max;
            _entity = entity;
        }

     
        public void TakeDamage(float amount)
        {
            if (Value <= 0 || Value <= 0)
                return;

            Value -= amount;

            if (Value <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            Value = 0;
            _entity.Destroy();
        }
    }
}
