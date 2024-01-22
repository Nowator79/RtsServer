using System.Diagnostics;

namespace RtsServer.App.Tools
{
    public class TimeSystem
    {
        private double LastUpdate { get; set; }
        public TimeSystem()
        {
            Update();
        }

        public void Update()
        {
            LastUpdate = DateTime.Now.Ticks;
        }

        public double GetDetlta()
        {
            return new TimeSpan((long)(DateTime.Now.Ticks - LastUpdate)).TotalSeconds;
        }
    }
}
