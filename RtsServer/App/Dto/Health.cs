namespace RtsServer.App.Battle.Dto
{
    public struct Health
    {
        public int Value;
        public int Max;

        public Health(int value, int max)
        {
            Value = value;
            Max = max;
        }

        public Health(int max)
        {
            Value = max;
            Max = max;
        }
    }
}
