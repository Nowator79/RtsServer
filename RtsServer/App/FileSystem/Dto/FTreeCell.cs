namespace RtsServer.App.FileSystem.Dto
{
    /// <summary>Ненулевая клетка дерева в файле карты (sparse). Variant &gt; 0.</summary>
    public struct FTreeCell
    {
        public int X { get; set; }
        public int Y { get; set; }
        /// <summary>1 = сосна (пока); дальше другие виды.</summary>
        public int Variant { get; set; }

        public FTreeCell(int x, int y, int variant)
        {
            X = x;
            Y = y;
            Variant = variant;
        }
    }
}
