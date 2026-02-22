using System.Diagnostics;

namespace RtsServer.App.Tools
{
    public class TimeSystem
    {
        private readonly Stopwatch timer = Stopwatch.StartNew(); // Запускаем таймер
        private TimeSpan previousElapsed = TimeSpan.Zero; // Предыдущее время
        private double deltaMilliseconds = 0.0; // Дельта в миллисекундах

        public TimeSystem()
        {
            previousElapsed = timer.Elapsed;
        }

        public void Update()
        {
            var currentElapsed = timer.Elapsed;
            deltaMilliseconds = (currentElapsed - previousElapsed).TotalMilliseconds;
            previousElapsed = currentElapsed;
        }

        /// <summary>
        /// Время между кадрами (в секундах)
        /// </summary>
        public double GetDelta()
        {
            return deltaMilliseconds / 1000.0;
        }

        /// <summary>
        /// Общее прошедшее время с момента запуска таймера (в секундах)
        /// </summary>
        public double GetTime()
        {
            return timer.Elapsed.TotalSeconds;
        }
    }
}
