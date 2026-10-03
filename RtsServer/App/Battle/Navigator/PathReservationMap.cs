using RtsServer.App.Battle.Dto;

namespace RtsServer.App.Battle.Navigator
{
    /// <summary>
    /// Лёгкая soft-резервация: если коридор уже плотный — чуть дороже, но без принудительных боковых полос.
    /// </summary>
    public sealed class PathReservationMap
    {
        public const int Capacity = 3;
        public const int Penalty = 3;
        public const int OverCapacityPenalty = 6;
        public const int TrimEnds = 2;

        private readonly Dictionary<Vector2Int, int> _counts = new();

        public int GetExtraCost(Vector2Int cell)
        {
            if (!_counts.TryGetValue(cell, out int n) || n <= 0)
                return 0;

            int cost = n * Penalty;
            if (n >= Capacity)
                cost += OverCapacityPenalty * (n - Capacity + 1);
            return cost;
        }

        public void AddPath(IEnumerable<Vector2Int> path)
        {
            if (path == null) return;

            List<Vector2Int> cells = path as List<Vector2Int> ?? path.ToList();
            if (cells.Count == 0) return;

            int from = Math.Min(TrimEnds, cells.Count);
            int to = Math.Max(from, cells.Count - TrimEnds);
            for (int i = from; i < to; i++)
            {
                Vector2Int cell = cells[i];
                _counts.TryGetValue(cell, out int n);
                _counts[cell] = n + 1;
            }
        }

        // Совместимость со старыми вызовами GroupMoveAssigner.
        public void SetLane(int laneIndex, Vector2Int from, Vector2Int to) { }

        public void ClearLane() { }
    }
}
