namespace RtsServer.App.FileSystem.Dto
{
    public struct FMap
    {
        public List<FTile> Chunks { get; set; }
        public int Width { get; set; }
        public int Length { get; set; }
        public string Name { get; set; }

        /// <summary>Только клетки с рудой &gt; 0. Отсутствует/пусто — сгенерировать дефолт.</summary>
        public List<FOreCell>? Ore { get; set; }

        /// <summary>Sparse деревья (Variant &gt; 0). Отсутствует/пусто — без леса.</summary>
        public List<FTreeCell>? Trees { get; set; }
    }
}
