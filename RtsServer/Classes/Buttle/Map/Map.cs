using RtsServer.Classes.Buttle.Map.ChunksType;
using RtsServer.Classes.Buttle.Unit;
using System.Drawing;

namespace RtsServer.Classes.Buttle.Map
{
    public class MapBase
    {
        public List<ChunkBase> Chunks { get; set; }
        public int Size { get; private set; }
        public ChunkPool ChunkPool { get; set; }
        public List<UnitBase> Units { get; set; }

        public MapBase(int size)
        {
            Size = size;
            Chunks = new();
        }

    }
}
