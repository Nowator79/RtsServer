using System.Diagnostics;

namespace RtsServer.App.Tools
{
    public class TimeSystem
    {
        private Stopwatch timer = Stopwatch.StartNew(); // Запускаем таймер
        private TimeSpan previousElapsed = TimeSpan.Zero; // Предыдущее время
        private double deltaMilliseconds = 0.0; // Дельта в миллисекундах

        public TimeSystem()
        {
            // Инициализация предыдущего времени текущим значением таймера
            previousElapsed = timer.Elapsed;
        }

        public void Update()
        {
            var currentElapsed = timer.Elapsed; // Текущее время
            // Вычисляем разницу между текущим и предыдущим временем
            deltaMilliseconds = (currentElapsed - previousElapsed).TotalMilliseconds;
            // Обновляем предыдущее время для следующего вызова
            previousElapsed = currentElapsed;
        }

        public double GetDelta()
        {
            return deltaMilliseconds / 1000; // Возвращаем дельту в миллисекундах
        }
    }
}