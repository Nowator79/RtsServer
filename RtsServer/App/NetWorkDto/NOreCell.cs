namespace RtsServer.App.NetWorkDto
{
    public struct NOreCell
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Amount { get; set; }

        public NOreCell(int x, int y, int amount)
        {
            X = x;
            Y = y;
            Amount = amount;
        }
    }
}
