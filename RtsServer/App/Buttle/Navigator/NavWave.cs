using RtsServer.App.Buttle.Dto;
using RtsServer.App.Buttle.MapButlle;
using RtsServer.App.Buttle.MapButlle.ChunksType;
using System.Drawing;

namespace RtsServer.App.Buttle.Navigator
{
    public class NavWave
    {
        private Map _map;
        private NavChunk[,] _mapChunks;
        private Vector2Int _startPoint;
        private Vector2Int _endPoint { get; set; }

        private List<Vector2Int> routeNavigation = new();
        private bool _isFinish = false;

        private HashSet<Vector2Int> _isCheckPoints;
        private HashSet<Vector2Int> _isProcessPoints;


        public NavWave(
            Map map,
            Vector2Int startPoint,
            Vector2Int endPoint
            )
        {
            _isCheckPoints = new();
            _isProcessPoints = new();
            _map = map;
            NavChunk[,] chunks = new NavChunk[map.Width, map.Length];

            ChunkBase[,] mapArray = map.GetArrayMap();

            for (int x = 0; x < map.Width; x++)
            {
                for (int y = 0; y < map.Length; y++)
                {
                    chunks[x, y] = new NavChunk(new Vector2Int(x, y), mapArray[x, y].Height, mapArray[x, y].Id);
                }
            }

            _mapChunks = chunks;
            _startPoint = startPoint;
            _endPoint = endPoint;
        }


        private void ProcessChunk(Vector2Int point, int step = 0)
        {
            _isProcessPoints.Add(point);

            _mapChunks[point.X, point.Y].StepsCount = step;
            HashSet<NavChunk> chunksTmp = NavHelper.GetNavChunksByPoints(
                NavHelper.GetSafeNear(point, _map.Width, _map.Length), _mapChunks
                ).ToHashSet();

            HashSet<NavChunk> forRemoveChunks = new();
            foreach (NavChunk item in chunksTmp)
            {
                if (!NavHelper.CanMove(_mapChunks[point.X, point.Y], item))
                {
                    forRemoveChunks.Add(item);
                }
            }

            foreach (NavChunk item in forRemoveChunks)
            {
                chunksTmp.Remove(item);
            }

            NavChunk[] nearChunks = NavHelper.GetSortByDistanceChunk(chunksTmp.ToArray());

            foreach (NavChunk nearPoint in nearChunks)
            {
                if (!_isProcessPoints.Contains(nearPoint.Position))
                {
                    ProcessChunk(nearPoint.Position, ++step);
                }
            }

        }

        private void ReversProcessChunk(NavChunk chunk)
        {
            HashSet<NavChunk> chunksTmp = NavHelper.GetNavChunksByPoints(
             NavHelper.GetSafeNear(chunk.Position, _map.Width, _map.Length), _mapChunks
             ).ToHashSet();
            HashSet<NavChunk> nearChunkTmp = NavHelper.GetSortForReverseChunk(chunksTmp.ToArray()).ToHashSet();

            HashSet<NavChunk> forRemoveChunks = new();
            foreach (NavChunk item in chunksTmp)
            {
                if (!NavHelper.CanMove(chunk, item))
                {
                    forRemoveChunks.Add(item);
                }
            }

            foreach (NavChunk item in forRemoveChunks)
            {
                nearChunkTmp.Remove(item);
            }
            if (nearChunkTmp.Count == 0)
            {
                _isFinish = true;
                return;
            }

            NavChunk nearChunk = nearChunkTmp.First();
            routeNavigation.Add(nearChunk.Position);
            if (nearChunk.Position == _startPoint)
            {
                _isFinish = true;
            }
            if (_isFinish)
            {
                return;
            }
            ReversProcessChunk(nearChunk);
        }

        public void Run()
        {
            // OpenStartPoint

            for (int x = 0; x < _map.Width; x++)
            {
                for (int y = 0; y < _map.Length; y++)
                {
                    Vector2Int point = new(x, y);
                    _mapChunks[point.X, point.Y].TargetRange = NavHelper.DistanceSQRT(point, _endPoint);
                }
            }

            // рекурсивный анализ карты
            ProcessChunk(new Vector2Int(_startPoint.X, _startPoint.Y));

            // рекурсивынй обратный поиск пути
            routeNavigation.Add(_endPoint);
            ReversProcessChunk(_mapChunks[_endPoint.X, _endPoint.Y]);

            return;
            Console.Clear();
            Console.WriteLine("StepsCount:");
            Console.Write($"[ {"x",3} ] ");
            for (int y = 0; y < _map.Length; y++)
            {
                Console.Write($"[ {y,3} ] ");
            }
            Console.WriteLine();

            for (int x = 0; x < _map.Width; x++)
            {
                Console.Write($"[ {x,3} ] ");

                for (int y = 0; y < _map.Length; y++)
                {
                    Console.Write($"[ {_mapChunks[x, y].StepsCount,3} ] ");
                }
                Console.WriteLine();
            }
            Console.WriteLine();

            Console.WriteLine("Height:");
            Console.Write($"[ {"x",3} ] ");
            for (int y = 0; y < _map.Length; y++)
            {
                Console.Write($"[ {y,3} ] ");
            }
            Console.WriteLine();

            for (int x = 0; x < _map.Width; x++)
            {
                Console.Write($"[ {x,3} ] ");

                for (int y = 0; y < _map.Length; y++)
                {
                    Console.Write($"[ {_mapChunks[x, y].Height,3} ] ");
                }
                Console.WriteLine();
            }
            Console.WriteLine();

            Console.WriteLine("TargetRange:");
            Console.Write($"[ {"x",3} ] ");
            for (int y = 0; y < _map.Length; y++)
            {
                Console.Write($"[ {y,3} ] ");
            }
            Console.WriteLine();
            for (int x = 0; x < _map.Width; x++)
            {
                Console.Write($"[ {x,3} ] ");

                for (int y = 0; y < _map.Length; y++)
                {
                    Console.Write($"[ {_mapChunks[x, y].TargetRange,3} ] ");
                }
                Console.WriteLine();
            }
            Console.WriteLine();



        
            for (int x = 0; x < _map.Width; x++)
            {
                for (int y = 0; y < _map.Length; y++)
                {
                    string t = " ";
                    if (routeNavigation.Contains(_mapChunks[x, y].Position))
                    {
                        t = "x";

                    }
                    else
                    {

                    }
                    Console.Write($"[{t}] ");
                }
                Console.WriteLine();
            }
            Console.WriteLine();

        }

        public List<Vector2Int> GetRoutPath() => routeNavigation;
    }
}
