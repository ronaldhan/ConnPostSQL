using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Threading;

namespace ConnPostSQL
{
    class Timing
    {
        TimeSpan startingTime;
        TimeSpan duration;
        Stopwatch stw;
        string Span;

        public Timing()
        {
            startingTime = new TimeSpan(0);
            duration = new TimeSpan(0);
            stw = new Stopwatch();
        }
        public void stopTime()
        {
            duration = Process.GetCurrentProcess().Threads[0].UserProcessorTime.Subtract(startingTime);
        }
        public void startTime()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            startingTime = Process.GetCurrentProcess().Threads[0].UserProcessorTime;
        }
        //public TimeSpan Result()
        //{
        //    return duration;
        //}

        public void Start()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            stw.Reset();
            stw.Start();
        }

        public void Stop()
        {
            stw.Stop();
            Span = stw.ElapsedMilliseconds.ToString() + "ms";
        }

        public string Result()
        {
            return Span;
        }

    }
}
