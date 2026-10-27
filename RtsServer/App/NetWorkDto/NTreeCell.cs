namespace RtsServer.App.NetWorkDto
{
    public struct NTreeCell
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Variant { get; set; }

        public NTreeCell(int x, int y, int variant)
        {
            X = x;
            Y = y;
            Variant = variant;
        }
    }
}
