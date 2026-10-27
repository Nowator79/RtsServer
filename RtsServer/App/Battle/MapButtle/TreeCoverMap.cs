using RtsServer.App.FileSystem.Dto;

namespace RtsServer.App.Battle.MapBattle
{
    /// <summary>
    /// Sparse-покрытие деревьями: клетки без дерева ключа не имеют.
    /// Значение — variantId (&gt;0).
    /// </summary>
    public sealed class TreeCoverMap
    {
        private readonly Dictionary<long, int> _cells = new();

        public int Count => _cells.Count;

        public static long Pack(int x, int y) => ((long)x << 32) | (uint)y;

        public static void Unpack(long key, out int x, out int y)
        {
            x = (int)(key >> 32);
            y = (int)(key & 0xFFFFFFFF);
        }

        public int Get(int x, int y) =>
            _cells.TryGetValue(Pack(x, y), out int variant) ? variant : 0;

        public bool HasTree(int x, int y) => Get(x, y) > 0;

        public void Set(int x, int y, int variant)
        {
            long key = Pack(x, y);
            if (variant <= 0)
                _cells.Remove(key);
            else
                _cells[key] = variant;
        }

        public void Clear() => _cells.Clear();

        public void LoadFrom(IEnumerable<FTreeCell>? cells)
        {
            _cells.Clear();
            if (cells == null)
                return;
            foreach (FTreeCell c in cells)
            {
                if (c.Variant > 0)
                    _cells[Pack(c.X, c.Y)] = c.Variant;
            }
        }

        public List<FTreeCell> ToFileList()
        {
            var list = new List<FTreeCell>(_cells.Count);
            foreach (KeyValuePair<long, int> kv in _cells)
            {
                Unpack(kv.Key, out int x, out int y);
                list.Add(new FTreeCell(x, y, kv.Value));
            }
            return list;
        }

        public IEnumerable<(int X, int Y, int Variant)> Enumerate()
        {
            foreach (KeyValuePair<long, int> kv in _cells)
            {
                Unpack(kv.Key, out int x, out int y);
                yield return (x, y, kv.Value);
            }
        }
    }
}
