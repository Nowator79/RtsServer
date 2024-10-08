﻿using RtsServer.App.Battle.Dto;
using RtsServer.App.Battle.MapButlle;
using RtsServer.App.Battle.MapButlle.ChunksType;

namespace RtsServer.App.Battle.Navigator
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

        private HashSet<Vector2Int> _pollCurPoints;
        private HashSet<Vector2Int> _pollNextPoints;

        public bool IsFail { get; private set; } = false;
        public NavWave(
            Map map,
            Vector2Int startPoint,
            Vector2Int endPoint
            )
        {
            _isCheckPoints = new();
            _isProcessPoints = new();

            _pollCurPoints = new();
            _pollNextPoints = new();

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


        private void ProcessChunk(Vector2Int curPoint, int step = 0)
        {
            if (_isProcessPoints.Contains(curPoint)) return;
            _isProcessPoints.Add(curPoint);

            _mapChunks[curPoint.X, curPoint.Y].StepsCount = step;

            HashSet<NavChunk> nearChunksHS = NavHelper.GetNavChunksByPoints(
                NavHelper.GetSafeNear(curPoint, _map.Width, _map.Length), _mapChunks
                ).ToHashSet();

            HashSet<NavChunk> forRemoveChunks = new();
            foreach (NavChunk nearChunk in nearChunksHS)
            {
                if (!CanMove(nearChunk.Position.X, nearChunk.Position.Y))
                {
                    forRemoveChunks.Add(nearChunk);
                }
            }

            foreach (NavChunk item in forRemoveChunks)
            {
                nearChunksHS.Remove(item);
            }

            NavChunk[] nearChunks = NavHelper.GetSortByDistanceChunk(nearChunksHS.ToArray());
            foreach (NavChunk nearChunk in nearChunks)
            {
                _pollNextPoints.Add(nearChunk.Position);
            }
        }
        private int reversCount = 200;
        private void ReversProcessChunk(NavChunk curChunk)
        {
            if (reversCount-- < 0)
            {
                return;
            }
            HashSet<NavChunk> chunksTmp = NavHelper.GetNavChunksByPoints(
             NavHelper.GetSafeNear(curChunk.Position, _map.Width, _map.Length), _mapChunks
             ).ToHashSet();

            chunksTmp.RemoveWhere(itemChunk => !CanMove(itemChunk.Position.X, itemChunk.Position.Y) && !(_startPoint.X  == itemChunk.Position.X && _startPoint.Y == itemChunk.Position.Y));

            NavChunk nearChunk = NavHelper.GetSortForReverseChunk(chunksTmp.ToArray(), curChunk.Position.X, curChunk.Position.Y);

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
            if(!CanMove(_endPoint.X, _endPoint.Y))
            {
                IsFail = true;
                return;
            }

            for (int x = 0; x < _map.Width; x++)
            {
                for (int y = 0; y < _map.Length; y++)
                {
                    Vector2Int point = new(x, y);
                    _mapChunks[point.X, point.Y].TargetRange = NavHelper.DistanceSQRT(point, _endPoint);
                }
            }

            // анализ карты
            Vector2Int startPoint = new(_startPoint.X, _startPoint.Y);
            int step = 0;
            _pollCurPoints.Add(startPoint);
            while (_pollCurPoints.Count > 0)
            {
                foreach (Vector2Int curPoint in _pollCurPoints)
                {
                    ProcessChunk(curPoint, step);
                }
                _pollCurPoints.Clear();
                foreach (Vector2Int nextPoint in _pollNextPoints)
                {
                    _pollCurPoints.Add(nextPoint);
                }
                _pollNextPoints.Clear();
                step++;
            }

            // рекурсивынй обратный поиск пути
            routeNavigation.Add(_endPoint);
            ReversProcessChunk(_mapChunks[_endPoint.X, _endPoint.Y]);
            if (ConfigGameServer.IsDebugGameNavUpdate)
            {
                const int countN = 4;
                if (ConfigGameServer.IsEnabledClearConsole) Console.Clear();
                Console.WriteLine("StepsCount:");
                Console.Write($"[{"x",countN}]");
                for (int y = 0; y < _map.Length; y++)
                {
                    Console.Write($"[{y,countN}]");
                }
                Console.WriteLine();

                for (int x = 0; x < _map.Width; x++)
                {
                    Console.Write($"[{x,countN}]");

                    for (int y = 0; y < _map.Length; y++)
                    {
                        Console.Write($"[{_mapChunks[x, y].StepsCount,countN}]");
                    }
                    Console.WriteLine();
                }
                Console.WriteLine();

                Console.WriteLine("Height:");
                Console.Write($"[{"x",countN}] ");
                for (int y = 0; y < _map.Length; y++)
                {
                    Console.Write($"[{y,countN}]");
                }
                Console.WriteLine();

                for (int x = 0; x < _map.Width; x++)
                {
                    Console.Write($"[{x,countN}] ");

                    for (int y = 0; y < _map.Length; y++)
                    {
                        Console.Write($"[{_mapChunks[x, y].Height,countN} ]");
                    }
                    Console.WriteLine();
                }
                Console.WriteLine();

                Console.WriteLine("TargetRange:");
                Console.Write($"[{"x",countN}]");
                for (int y = 0; y < _map.Length; y++)
                {
                    Console.Write($"[{y,countN}]");
                }
                Console.WriteLine();
                for (int x = 0; x < _map.Width; x++)
                {
                    Console.Write($"[{x,countN}]");

                    for (int y = 0; y < _map.Length; y++)
                    {
                        Console.Write($"[{_mapChunks[x, y].TargetRange,countN}]");
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
                        Console.Write($"[{t}]");
                    }
                    Console.WriteLine();
                }
                Console.WriteLine();
            }

        }

        public List<Vector2Int> GetRoutPath() => routeNavigation;

        public bool CanMove(int X, int Y)
        {
            var tmp = _map.GetArrayMap()[X, Y];
            return tmp.Height == 1 && tmp.UnitsInPoint.Count == 0;
        }
    }
}
