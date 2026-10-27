using RtsServer.App.FileSystem.Dto;

namespace RtsServer.App.NetWorkDto
{
    public struct NMap
    {
        public NMap(int width, int length) : this()
        {
            Width = width;
            Length = length;
        }

        public NMap(List<NTile> chunks, int width, int length, string name)
        {
            Tiles = chunks;
            Width = width;
            Length = length;
            Name = name;
        }

        public List<NTile> Tiles { get; set; }
        public int Width { get; set; }
        public int Length { get; set; }
        public string Name { get; set; }
        public string Code { get; set; }

        /// <summary>Sparse руда для визуала (только ненулевые клетки).</summary>
        public List<NOreCell>? Ore { get; set; }

        /// <summary>Sparse деревья (Variant &gt; 0).</summary>
        public List<NTreeCell>? Trees { get; set; }
    }
}
