namespace RtsServer.App.FileSystem.Dto
{
    /// <summary>Ненулевая клетка руды в файле карты (sparse).</summary>
    public struct FOreCell
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Amount { get; set; }

        public FOreCell(int x, int y, int amount)
        {
            X = x;
            Y = y;
            Amount = amount;
        }
    }
}
