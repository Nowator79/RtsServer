using RtsServer.Classes.Buttle.Map.ChunksType;

namespace RtsServer.Classes.Buttle.Map
{
    public struct Chank
    {
        public int X { get; private set; }
        public int Y { get; private set; }
        public int Level { get; private set; }
        public ITypeChunk TypeChunk { get; private set; }

    }
}
