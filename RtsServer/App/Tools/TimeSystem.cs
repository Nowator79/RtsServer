using System.Diagnostics;

namespace RtsServer.App.Tools
{
    public class TimeSystem
    {
        private readonly Stopwatch timer = Stopwatch.StartNew();
        private TimeSpan previousElapsed = TimeSpan.Zero;
        private double deltaMilliseconds = 0.0;

        /// <summary>В реальной игре ограничиваем скачок dt (лаг → пропуск клеток).</summary>
        public const double MaxDeltaSeconds = 0.1;

        private bool _fixedMode;
        private double _fixedDeltaSeconds = 0.05;
        private double _fixedTime;

        public TimeSystem()
        {
            previousElapsed = timer.Elapsed;
        }

        /// <summary>Ускоренная/детерминированная симуляция: каждый Update даёт фиксированный dt.</summary>
        public void EnableFixedStep(double deltaSeconds)
        {
            _fixedMode = true;
            _fixedDeltaSeconds = Math.Max(0.001, deltaSeconds);
            _fixedTime = 0;
            deltaMilliseconds = _fixedDeltaSeconds * 1000.0;
        }

        public void DisableFixedStep()
        {
            _fixedMode = false;
            previousElapsed = timer.Elapsed;
        }

        public void Update()
        {
            if (_fixedMode)
            {
                deltaMilliseconds = _fixedDeltaSeconds * 1000.0;
                _fixedTime += _fixedDeltaSeconds;
                return;
            }

            var currentElapsed = timer.Elapsed;
            deltaMilliseconds = (currentElapsed - previousElapsed).TotalMilliseconds;
            previousElapsed = currentElapsed;

            double maxMs = MaxDeltaSeconds * 1000.0;
            if (deltaMilliseconds > maxMs)
                deltaMilliseconds = maxMs;
        }

        public double GetDelta() => deltaMilliseconds / 1000.0;

        public double GetTime() => _fixedMode ? _fixedTime : timer.Elapsed.TotalSeconds;
    }
}
