using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MeteringFramework
{
    public class Race
    {
        private readonly Action[] _racers;
        private readonly int _turns;
        private readonly int _count;
        private readonly Action<Progress> _progress;

        private long[][] _times; // [racer][turn]

        public Race(Action[] racers, int turns, int count, Action<Progress> progress)
        {
            _racers = racers;
            _turns = turns;
            _count = count;
            _progress = progress;
        }

        public virtual Task<RaceResult[]> RunAsync(CancellationToken cancellationToken = default)
        {
            _progress(new Progress("Initializing."));
            _times = new long[_racers.Length][];
            for (int i = 0; i < _racers.Length; i++)
                _times[i] = new long[_turns];

            for (int turn = 0; turn < _turns; turn++)
            {
                for (int racer = 0; racer < _racers.Length; racer++)
                {
                    if(cancellationToken != default)
                        cancellationToken.ThrowIfCancellationRequested();

                    // Atomic measuring
                    AggregatedProgress($"Turn {turn + 1}/{_turns}, racer {racer + 1}");
                    var stopwatch = Stopwatch.StartNew();
                    for (int i = 0; i < _count; i++)
                    {
                        _racers[racer]();
                    }
                    _times[racer][turn] = stopwatch.ElapsedTicks;
                }
            }

            _progress(new Progress($"Race finished. Calculating result."));

            var totalTime = _times.Select(x => x.Sum()).Sum();
            return Task.FromResult(_times.Select(x => CreateResult(x, totalTime)).ToArray());
        }

        private DateTime _lastAggregatedProgress = DateTime.MinValue;
        private void AggregatedProgress(string message)
        {
            if (DateTime.Now - _lastAggregatedProgress < TimeSpan.FromSeconds(1))
                return;

            _progress(new Progress(message));
            _lastAggregatedProgress = DateTime.Now;
        }

        private RaceResult CreateResult(long[] times, long totalTime)
        {
            var percent = (times.Sum() * 100.0d) / (totalTime * 1.0d);
            return new RaceResult
            {
                MinTime = new TimeSpan(times.Min()),
                MaxTime = new TimeSpan(times.Max()),
                AverageTime = new TimeSpan(Convert.ToInt64(times.Average())),
                Percent = percent,
            };
        }
    }
}
