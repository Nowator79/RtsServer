using System.Diagnostics;

namespace RtsServer.App.Tools
{
    public class TimeSystem
    {
        private DateTime LastUpdate { get; set; }
        private double LastReuslt { get; set; }
        public TimeSystem()
        {
            Update();
        }

        public void Update()
        {
            LastUpdate = DateTime.Now;
            LastReuslt = 0;
        }

        public double GetDetlta()
        {
            if(LastReuslt != 0) return LastReuslt;
            TimeSpan interval = DateTime.Now - LastUpdate;
            double res = (double)interval.Ticks / (double)1000;
            if (ConfigGameServer.IsDebugTimeUpdate)
            {
                Console.WriteLine(res);
            }
            LastReuslt = res;
            return res;
        }
    }
}
