using System;

namespace MeteringFramework
{
    public class RaceResult
    {
        public TimeSpan MinTime { get; internal set; }
        public TimeSpan MaxTime { get; internal set; }
        public TimeSpan AverageTime { get; internal set; }
        public double Percent { get; internal set; }
    }
}
