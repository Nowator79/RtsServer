using System.Diagnostics;

namespace RtsServer.App.Tools
{
    public class TimeSystem
    {
        private DateTime LastUpdate { get; set; }
        public TimeSystem()
        {
            Update();
        }

        public void Update()
        {
            LastUpdate = DateTime.Now;
        }

        public double GetDetlta()
        {
            TimeSpan interval = DateTime.Now - LastUpdate;
            return (double)interval.Ticks / (double)1000;
        }
    }
}
