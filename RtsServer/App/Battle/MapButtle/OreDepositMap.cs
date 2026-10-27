using RtsServer.App.FileSystem.Dto;

namespace RtsServer.App.Battle.MapBattle
{
    /// <summary>
    /// Sparse-запасы руды: клетки без руды ключа не имеют (не храним нули).
    /// </summary>
    public sealed class OreDepositMap
    {
        private readonly Dictionary<long, int> _cells = new();
        private readonly HashSet<long> _dirty = new();

        public int Count => _cells.Count;
        public bool HasDirty => _dirty.Count > 0;

        public static long Pack(int x, int y) => ((long)x << 32) | (uint)y;

        public static void Unpack(long key, out int x, out int y)
        {
            x = (int)(key >> 32);
            y = (int)(key & 0xFFFFFFFF);
        }

        public int Get(int x, int y) =>
            _cells.TryGetValue(Pack(x, y), out int amount) ? amount : 0;

        public void Set(int x, int y, int amount)
        {
            long key = Pack(x, y);
            if (amount <= 0)
                _cells.Remove(key);
            else
                _cells[key] = amount;
            _dirty.Add(key);
        }

        public void Clear()
        {
            foreach (long key in _cells.Keys)
                _dirty.Add(key);
            _cells.Clear();
        }

        /// <summary>Забрать до maxAmount. Возвращает фактически взятое.</summary>
        public int Extract(int x, int y, int maxAmount)
        {
            if (maxAmount <= 0)
                return 0;
            long key = Pack(x, y);
            if (!_cells.TryGetValue(key, out int have) || have <= 0)
                return 0;

            int take = Math.Min(have, maxAmount);
            int left = have - take;
            if (left <= 0)
                _cells.Remove(key);
            else
                _cells[key] = left;
            _dirty.Add(key);
            return take;
        }

        /// <summary>Снимает dirty: Amount=0 — клетка опустела.</summary>
        public List<RtsServer.App.NetWorkDto.NOreCell> DrainDirty()
        {
            var list = new List<RtsServer.App.NetWorkDto.NOreCell>(_dirty.Count);
            foreach (long key in _dirty)
            {
                Unpack(key, out int x, out int y);
                int amount = _cells.TryGetValue(key, out int a) ? a : 0;
                list.Add(new RtsServer.App.NetWorkDto.NOreCell(x, y, amount));
            }
            _dirty.Clear();
            return list;
        }

        /// <summary>Полный sparse-снимок для force-full синка.</summary>
        public List<RtsServer.App.NetWorkDto.NOreCell> SnapshotAll()
        {
            var list = new List<RtsServer.App.NetWorkDto.NOreCell>(_cells.Count);
            foreach (KeyValuePair<long, int> kv in _cells)
            {
                Unpack(kv.Key, out int x, out int y);
                list.Add(new RtsServer.App.NetWorkDto.NOreCell(x, y, kv.Value));
            }
            _dirty.Clear();
            return list;
        }

        public void LoadFrom(IEnumerable<FOreCell>? cells)
        {
            _cells.Clear();
            _dirty.Clear();
            if (cells == null)
                return;
            foreach (FOreCell c in cells)
            {
                if (c.Amount > 0)
                    _cells[Pack(c.X, c.Y)] = c.Amount;
            }
        }

        public List<FOreCell> ToFileList()
        {
            var list = new List<FOreCell>(_cells.Count);
            foreach (KeyValuePair<long, int> kv in _cells)
            {
                Unpack(kv.Key, out int x, out int y);
                list.Add(new FOreCell(x, y, kv.Value));
            }
            return list;
        }

        public IEnumerable<(int X, int Y, int Amount)> Enumerate()
        {
            foreach (KeyValuePair<long, int> kv in _cells)
            {
                Unpack(kv.Key, out int x, out int y);
                yield return (x, y, kv.Value);
            }
        }

        /// <summary>Если в файле руды нет — редкие пятна на земле (обратная совместимость).</summary>
        public void GenerateDefaultPatches(Map map, int seed = 0)
        {
            _cells.Clear();
            if (map == null)
                return;

            var rng = new Random(seed != 0 ? seed : map.Width * 73856093 ^ map.Length * 19349663);
            var chunks = map.GetArrayMap();
            int patches = Math.Max(3, (map.Width * map.Length) / 400);

            for (int p = 0; p < patches; p++)
            {
                int cx = rng.Next(2, Math.Max(3, map.Width - 2));
                int cy = rng.Next(2, Math.Max(3, map.Length - 2));
                if (chunks[cx, cy].Id == 0 || chunks[cx, cy].Height <= 0)
                    continue;

                int radius = rng.Next(1, 4);
                int baseAmount = rng.Next(900, 1800);
                for (int x = cx - radius; x <= cx + radius; x++)
                {
                    for (int y = cy - radius; y <= cy + radius; y++)
                    {
                        if (x < 0 || y < 0 || x >= map.Width || y >= map.Length)
                            continue;
                        if (chunks[x, y].Id == 0 || chunks[x, y].Height <= 0)
                            continue;
                        int dx = x - cx;
                        int dy = y - cy;
                        if (dx * dx + dy * dy > radius * radius)
                            continue;
                        int amount = (int)(baseAmount * (0.55 + 0.45 * rng.NextDouble()));
                        Set(x, y, Get(x, y) + amount);
                    }
                }
            }
        }
    }
}
