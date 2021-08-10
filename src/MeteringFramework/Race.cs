using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MeteringFramework
{
    public class Race
    {
        private Action[] _racers;
        private int _turns;
        private int _count;
        private Action<Progress> _progress;
        private bool _parallelRacers;

        private long[][] _times; // [racer][turn]

        public Race(Action[] racers, int turns, int count, Action<Progress> progress, bool parallelRacers = false)
        {
            _racers = racers;
            _turns = turns;
            _count = count;
            _progress = progress;
            _parallelRacers = parallelRacers;
        }

        public virtual Task<RaceResult[]> RunAsync(CancellationToken cancellationToken = default)
        {
            if (_parallelRacers)
                return ParallelRunAsync(cancellationToken);

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

        private Task<RaceResult[]> ParallelRunAsync(CancellationToken cancellationToken)
        {
            _progress(new Progress("Initializing."));
            _times = new long[_racers.Length][];
            //for (int i = 0; i < _racers.Length; i++)
            //    _times[i] = new long[_turns];

            var tasks = new Task<long[]>[_racers.Length];
            for (int racerIndex = 0; racerIndex < _racers.Length; racerIndex++)
            {
                if (cancellationToken != default)
                    cancellationToken.ThrowIfCancellationRequested();

                // Atomic measuring
                var racerId = racerIndex + 1;
                var racer = _racers[racerIndex];
                tasks[racerIndex] = Task.Run<long[]>(
                    () => ParallelRun(racerId, racer, cancellationToken), cancellationToken);
            }

            Task.WaitAll(tasks);
            for (int racer = 0; racer < _racers.Length; racer++)
                _times[racer] = tasks[0].Result;

            _progress(new Progress($"Race finished. Calculating result."));

            var totalTime = _times.Select(x => x.Sum()).Sum();
            return Task.FromResult(_times.Select(x=> CreateResult(x, totalTime)).ToArray());
        }

        // one racer for parallel run
        private long[] ParallelRun(int racerId, Action racer, CancellationToken cancellationToken)
        {
            var times = new long[_turns];

            for (int turn = 0; turn < _turns; turn++)
            {
                if (cancellationToken != default)
                    cancellationToken.ThrowIfCancellationRequested();

                // Atomic measuring
                AggregatedProgress($"Turn {turn + 1}/{_turns}, racer {racerId}");
                var stopwatch = Stopwatch.StartNew();
                for (int i = 0; i < _count; i++)
                {
                    racer();
                }
                times[turn] = stopwatch.ElapsedTicks;
            }

            return times;
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
