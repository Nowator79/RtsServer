using RtsServer.App.Battle.Dto;
using RtsServer.App.Battle.Units;

namespace RtsServer.App.Battle.MapButlle.ChunksType
{
    public class ChunkBase
    {
        public ChunkBase(int id)
        {
            Id = id;
            UnitsInPoint = new();
        }

        public ChunkBase(int id, int height) : this(id)
        {
            Height = height;
        }

        public int Id { get; set; } = 1;
        public int Height { get; set; } = 1;
        public List<Unit> UnitsInPoint { get; set; }

    }
}
