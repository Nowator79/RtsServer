using System.Diagnostics;

namespace RtsServer.App.Tools
{
    public class TimeSystem
    {

        private Stopwatch timer = Stopwatch.StartNew();
        private TimeSpan dt = TimeSpan.FromSeconds(1.0 / 50.0);
        private TimeSpan elapsedTime = TimeSpan.Zero;

        public TimeSystem()
        {
            Update();
        }

        public void Update()
        {
            timer.Restart();
        }

        public double GetDelta()
        {
            elapsedTime = timer.Elapsed;

            return elapsedTime.TotalMilliseconds;
        }
    }
}
