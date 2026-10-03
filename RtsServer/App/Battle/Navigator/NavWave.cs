using RtsServer.App.Battle.Constructions;
using RtsServer.App.Battle.Dto;
using RtsServer.App.Battle.MapBattle;
using RtsServer.App.Battle.MapBattle.ChunksType;
using RtsServer.App.Battle.Units;

namespace RtsServer.App.Battle.Navigator
{
    /// <summary>
    /// A* по сетке. Без полной копии карты на каждый поиск (критично на больших картах).
    /// </summary>
    public sealed class NavWave : IDisposable
    {
        /// <summary>Кардинальный шаг дешевле диагонали — меньше зигзага.</summary>
        private const int CardinalStepCost = 10;
        private const int DiagonalStepCost = 14;
        /// <summary>Защита от патологического раздувания на огромных картах.</summary>
        private const int MaxExpansions = 120_000;

        private static readonly int[] NeighborDx = { -1, 0, 1, -1, 1, -1, 0, 1 };
        private static readonly int[] NeighborDy = { -1, -1, -1, 0, 0, 1, 1, 1 };

        private readonly Map _map;
        private readonly Vector2Int _startPoint;
        private readonly Vector2Int _endPoint;
        private readonly Unit? _self;
        private readonly Func<Vector2Int, int>? _extraCost;

        private readonly List<Vector2Int> _route = new();
        private Vector2Int?[,] _cameFrom = null!;
        private bool[,] _settled = null!;
        private int[,] _best = null!;
        private bool[,]? _constructionBlocked;
        private ChunkBase[,]? _tiles;

        private bool _disposed;

        public bool IsFail { get; private set; }

        public NavWave(
            Map map,
            Vector2Int startPoint,
            Vector2Int endPoint,
            Unit? self = null,
            Func<Vector2Int, int>? extraCost = null)
        {
            _map = map ?? throw new ArgumentNullException(nameof(map));
            _startPoint = startPoint;
            _endPoint = endPoint;
            _self = self;
            _extraCost = extraCost;
        }

        public void Run()
        {
            _tiles = _map.GetArrayMap();
            BuildConstructionBlocked();

            // Финиш должен быть проходимой землёй (стоячие чужие юниты — как раньше — блок).
            if (!CanEnter(_endPoint.X, _endPoint.Y, ignoreParkedUnits: false)
                && !IsStartCell(_endPoint.X, _endPoint.Y))
            {
                IsFail = true;
                return;
            }

            if (!CalculateAStar())
            {
                IsFail = true;
                return;
            }

            TraceBackPath();
        }

        public Queue<Vector2Int> GetRoutePath() => new(_route);

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _route.Clear();
            _cameFrom = null!;
            _settled = null!;
            _best = null!;
            _constructionBlocked = null;
            _tiles = null;
        }

        private void BuildConstructionBlocked()
        {
            int w = _map.Width;
            int h = _map.Length;
            _constructionBlocked = new bool[w, h];

            Game? game = _self?.Game;
            if (game == null)
                return;

            MarkConstructions(game.Constructions, w, h);
            MarkConstructions(game.ConstructionsForAdd, w, h);
        }

        private void MarkConstructions(IEnumerable<Construction> list, int w, int h)
        {
            foreach (Construction c in list)
            {
                if (c == null || c.IsDestroyed)
                    continue;

                Vector2Int origin = c.Position.ToInt();
                int sx = Math.Max(1, c.Size.X);
                int sy = Math.Max(1, c.Size.Y);
                int maxX = Math.Min(w, origin.X + sx);
                int maxY = Math.Min(h, origin.Y + sy);
                for (int x = Math.Max(0, origin.X); x < maxX; x++)
                {
                    for (int y = Math.Max(0, origin.Y); y < maxY; y++)
                        _constructionBlocked![x, y] = true;
                }
            }
        }

        private bool CalculateAStar()
        {
            int w = _map.Width;
            int h = _map.Length;
            _cameFrom = new Vector2Int?[w, h];
            _settled = new bool[w, h];
            _best = new int[w, h];

            // -1 = не посещали (вместо заполнения MaxValue — один проход всё равно нужен).
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                    _best[x, y] = -1;
            }

            var open = new PriorityQueue<Vector2Int, int>();
            _best[_startPoint.X, _startPoint.Y] = 0;
            open.Enqueue(_startPoint, Heuristic(_startPoint, _endPoint));

            int expansions = 0;

            while (open.Count > 0)
            {
                open.TryDequeue(out Vector2Int point, out _);
                if (_settled[point.X, point.Y])
                    continue;

                _settled[point.X, point.Y] = true;
                expansions++;
                if (expansions > MaxExpansions)
                    return false;

                int g = _best[point.X, point.Y];
                if (point.Equals(_endPoint))
                    return true;

                for (int i = 0; i < 8; i++)
                {
                    int nx = point.X + NeighborDx[i];
                    int ny = point.Y + NeighborDy[i];
                    if (nx < 0 || ny < 0 || nx >= w || ny >= h)
                        continue;
                    if (_settled[nx, ny])
                        continue;

                    bool diagonal = NeighborDx[i] != 0 && NeighborDy[i] != 0;
                    if (diagonal
                        && (!CanEnter(nx, point.Y, ignoreParkedUnits: false)
                            || !CanEnter(point.X, ny, ignoreParkedUnits: false)))
                        continue;

                    if (!CanEnter(nx, ny, ignoreParkedUnits: false))
                        continue;

                    int stepCost = (diagonal ? DiagonalStepCost : CardinalStepCost) + GetExtraCost(nx, ny);
                    if (stepCost < 1) stepCost = 1;

                    int ng = g + stepCost;
                    int prev = _best[nx, ny];
                    if (prev >= 0 && ng >= prev)
                        continue;

                    _best[nx, ny] = ng;
                    _cameFrom[nx, ny] = point;
                    int f = ng + Heuristic(new Vector2Int(nx, ny), _endPoint);
                    open.Enqueue(new Vector2Int(nx, ny), f);
                }
            }

            return _best[_endPoint.X, _endPoint.Y] >= 0;
        }

        private static int Heuristic(Vector2Int a, Vector2Int b)
        {
            int dx = Math.Abs(a.X - b.X);
            int dy = Math.Abs(a.Y - b.Y);
            // Octile, согласованный с 10/14.
            return 10 * (dx + dy) + (DiagonalStepCost - 2 * CardinalStepCost) * Math.Min(dx, dy);
        }

        private int GetExtraCost(int x, int y)
        {
            if (_extraCost == null) return 0;
            int extra = _extraCost(new Vector2Int(x, y));
            return extra > 0 ? extra : 0;
        }

        private void TraceBackPath()
        {
            _route.Clear();
            if (_best[_endPoint.X, _endPoint.Y] < 0)
                return;

            var stack = new Stack<Vector2Int>();
            Vector2Int current = _endPoint;
            stack.Push(current);

            int guard = _map.Width * _map.Length + 8;
            while (!current.Equals(_startPoint) && guard-- > 0)
            {
                Vector2Int? parent = _cameFrom[current.X, current.Y];
                if (parent == null)
                    break;
                current = parent.Value;
                stack.Push(current);
            }

            while (stack.Count > 0)
                _route.Add(stack.Pop());
        }

        private bool IsStartCell(int x, int y) => x == _startPoint.X && y == _startPoint.Y;

        private bool CanEnter(int x, int y, bool ignoreParkedUnits)
        {
            if (IsStartCell(x, y))
                return true;

            ChunkBase chunk = _tiles![x, y];
            if (chunk == null || chunk.Height != 1 || chunk.Id == 0)
                return false;

            if (_constructionBlocked != null && _constructionBlocked[x, y])
                return false;

            if (ignoreParkedUnits)
                return true;

            List<Unit>? units = chunk.UnitsInPoint;
            if (units == null || units.Count == 0)
                return true;

            for (int i = 0; i < units.Count; i++)
            {
                Unit? unit = units[i];
                if (unit == null || unit.IsDestroyed)
                    continue;
                if (_self != null && ReferenceEquals(unit, _self))
                    continue;
                if (!unit.OccupiesGroundCell)
                    continue;
                if (!unit.IsParked)
                    continue;
                return false;
            }

            return true;
        }
    }
}
