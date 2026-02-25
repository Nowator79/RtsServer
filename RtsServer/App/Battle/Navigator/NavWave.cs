using RtsServer.App.Battle.Dto;
using RtsServer.App.Battle.MapBattle;
using RtsServer.App.Battle.MapBattle.ChunksType;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RtsServer.App.Battle.Navigator
{
    public sealed class NavWave : IDisposable
    {
        private const int DebugColumnWidth = 4;
        private readonly Map _map;
        private readonly NavChunk[,] _mapChunks;
        private readonly Vector2Int _startPoint;
        private readonly Vector2Int _endPoint;

        private readonly List<Vector2Int> _route = new();
        private HashSet<Vector2Int> _processedPoints = new();
        private HashSet<Vector2Int> _currentWavePoints = new();
        private HashSet<Vector2Int> _nextWavePoints = new();

        private bool _isFinished;
        private bool _disposed;

        public bool IsFail { get; private set; }

        public NavWave(Map map, Vector2Int startPoint, Vector2Int endPoint)
        {
            _map = map ?? throw new ArgumentNullException(nameof(map));
            _startPoint = startPoint;
            _endPoint = endPoint;

            // Инициализация карты навигации
            _mapChunks = InitializeNavChunks(map);
        }

        public void Run()
        {
            if (!CanMove(_endPoint.X, _endPoint.Y))
            {
                IsFail = true;
                return;
            }

            CalculateWavePropagation();
            TraceBackPath();

            if (ConfigGameServer.IsDebugGameNavUpdate)
            {
                PrintDebugInfo();
            }
        }

        public Queue<Vector2Int> GetRoutePath()
        {
            _route.Reverse();
            var queue = new Queue<Vector2Int>(_route);
            _route.Reverse();
            return queue;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _processedPoints.Clear();
            _currentWavePoints.Clear();
            _nextWavePoints.Clear();
            _route.Clear();
        }

        private NavChunk[,] InitializeNavChunks(Map map)
        {
            var chunks = new NavChunk[map.Width, map.Length];
            var mapArray = map.GetArrayMap();

            for (int x = 0; x < map.Width; x++)
            {
                for (int y = 0; y < map.Length; y++)
                {
                    var c = new NavChunk(
                        new Vector2Int(x, y),
                        mapArray[x, y].Height,
                        mapArray[x, y].Id
                    );
                    c.StepsCount = -1;
                    chunks[x, y] = c;
                }
            }

            return chunks;
        }

        private void CalculateWavePropagation()
        {
            int step = 0;
            _currentWavePoints.Add(_startPoint);

            while (_currentWavePoints.Count > 0)
            {
                foreach (var point in _currentWavePoints)
                {
                    ProcessChunk(point, step);
                }

                if (_processedPoints.Contains(_endPoint))
                    break;

                (_currentWavePoints, _nextWavePoints) = (_nextWavePoints, _currentWavePoints);
                _nextWavePoints.Clear();
                step++;
            }
        }

        private void ProcessChunk(Vector2Int point, int step)
        {
            if (_processedPoints.Contains(point)) return;
            _processedPoints.Add(point);

            var chunk = _mapChunks[point.X, point.Y];
            chunk.StepsCount = step;
            chunk.TargetRange = NavHelper.DistanceSQRT(point, _endPoint);
            _mapChunks[point.X, point.Y] = chunk;

            var neighbors = GetValidNeighbors(point);
            foreach (var neighbor in neighbors)
            {
                _nextWavePoints.Add(neighbor.Position);
            }
        }

        private IEnumerable<NavChunk> GetValidNeighbors(Vector2Int point)
        {
            var nearPoints = NavHelper.GetSafeNear(point, _map.Width, _map.Length);
            var chunks = NavHelper.GetNavChunksByPoints(nearPoints, _mapChunks);
            return chunks.Where(chunk => CanMove(chunk.Position.X, chunk.Position.Y));
        }

        private void TraceBackPath()
        {
            _route.Add(_endPoint);
            TraceBackRecursive(_mapChunks[_endPoint.X, _endPoint.Y]);
        }

        private void TraceBackRecursive(NavChunk currentChunk, int maxDepth = 200)
        {
            if (maxDepth <= 0 || _isFinished) return;

            var neighbors = GetValidTracebackNeighbors(currentChunk);
            var nextChunk = NavHelper.GetSortForReverseChunk(neighbors, currentChunk.Position.X, currentChunk.Position.Y);

            _route.Add(nextChunk.Position);

            if (nextChunk.Position == _startPoint)
            {
                _isFinished = true;
                return;
            }

            TraceBackRecursive(nextChunk, maxDepth - 1);
        }

        private NavChunk[] GetValidTracebackNeighbors(NavChunk chunk)
        {
            var nearPoints = NavHelper.GetSafeNear(chunk.Position, _map.Width, _map.Length);
            var chunks = NavHelper.GetNavChunksByPoints(nearPoints, _mapChunks)
                .Where(c => c.Position == _startPoint ||
                            (StepsCountVisited(c) && CanMove(c.Position.X, c.Position.Y)))
                .ToArray();
            return chunks;
        }

        private bool StepsCountVisited(NavChunk c) => c.StepsCount >= 0;

        private bool CanMove(int x, int y)
        {
            var chunk = _map.GetArrayMap()[x, y];
            return chunk.Height == 1 && chunk.UnitsInPoint.Count == 0;
        }

        private void PrintDebugInfo()
        {
            if (ConfigGameServer.IsEnabledClearConsole)
                Console.Clear();

            PrintGrid("StepsCount:", c => c.StepsCount);
            PrintGrid("Height:", c => c.Height);
            PrintGrid("TargetRange:", c => Convert.ToInt32(c.TargetRange));
            PrintRoute();
        }

        private void PrintGrid(string title, Func<NavChunk, int> valueSelector)
        {
            Console.WriteLine(title);
            PrintHeader();

            for (int x = 0; x < _map.Width; x++)
            {
                Console.Write($"[{x,DebugColumnWidth}]");
                for (int y = 0; y < _map.Length; y++)
                {
                    Console.Write($"[{valueSelector(_mapChunks[x, y]),DebugColumnWidth}]");
                }
                Console.WriteLine();
            }
            Console.WriteLine();
        }

        private void PrintHeader()
        {
            Console.Write($"[{"x",DebugColumnWidth}]");
            for (int y = 0; y < _map.Length; y++)
            {
                Console.Write($"[{y,DebugColumnWidth}]");
            }
            Console.WriteLine();
        }

        private void PrintRoute()
        {
            var routeSet = new HashSet<Vector2Int>(_route);
            for (int x = 0; x < _map.Width; x++)
            {
                for (int y = 0; y < _map.Length; y++)
                {
                    var marker = routeSet.Contains(_mapChunks[x, y].Position) ? "X" : " ";
                    Console.Write($"[{marker}]");
                }
                Console.WriteLine();
            }
            Console.WriteLine();
        }
    }
}