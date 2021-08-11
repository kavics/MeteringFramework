using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MeteringFramework
{
    public enum GcUsage
    {
        None,
        GarbagePerTurn,
        GarbagePerIteration
    }

    public class Race
    {
        private readonly Action[] _racers;
        private readonly int _turns;
        private readonly int _count;
        private readonly Action<Progress> _progress;
        private readonly GcUsage _gcUsage;

        private long[][] _times; // [racer][turn]

        public Race(Action[] racers, int turns, int count, Action<Progress> progress, GcUsage gcUsage = GcUsage.None)
        {
            _racers = racers;
            _turns = turns;
            _count = count;
            _progress = progress;
            _gcUsage = gcUsage;
        }

        public async Task<RaceResult[]> RunAsync(CancellationToken cancel = default)
        {
            _progress(new Progress("Initializing."));
            _times = new long[_racers.Length][];
            for (int i = 0; i < _racers.Length; i++)
                _times[i] = new long[_turns];

            GC.Collect();
            switch (_gcUsage)
            {
                case GcUsage.None: await RunNoGcAsync(cancel); break;
                case GcUsage.GarbagePerTurn: await RunGarbagePerTurnAsync(cancel); break;
                case GcUsage.GarbagePerIteration: await RunGarbagePerIterationAsync(cancel); break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            _progress(new Progress($"Race finished. Calculating result."));

            var totalTime = _times.Select(x => x.Sum()).Sum();
            return _times.Select(x => CreateResult(x, totalTime)).ToArray();
        }
        private Task RunNoGcAsync(CancellationToken cancellationToken = default)
        {
            for (int turn = 0; turn < _turns; turn++)
            {
                for (int racer = 0; racer < _racers.Length; racer++)
                {
                    if (cancellationToken != default)
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
            return Task.CompletedTask;
        }
        private Task RunGarbagePerTurnAsync(CancellationToken cancellationToken = default)
        {
            for (int turn = 0; turn < _turns; turn++)
            {
                for (int racer = 0; racer < _racers.Length; racer++)
                {
                    if (cancellationToken != default)
                        cancellationToken.ThrowIfCancellationRequested();

                    // Atomic measuring
                    AggregatedProgress($"Turn {turn + 1}/{_turns}, racer {racer + 1}");
                    var stopwatch = Stopwatch.StartNew();
                    for (int i = 0; i < _count; i++)
                    {
                        _racers[racer]();
                    }
                    _times[racer][turn] = stopwatch.ElapsedTicks;

                    GC.Collect();
                }
            }
            return Task.CompletedTask;
        }
        private Task RunGarbagePerIterationAsync(CancellationToken cancellationToken = default)
        {
            for (int turn = 0; turn < _turns; turn++)
            {
                for (int racer = 0; racer < _racers.Length; racer++)
                {
                    if (cancellationToken != default)
                        cancellationToken.ThrowIfCancellationRequested();

                    // Atomic measuring
                    AggregatedProgress($"Turn {turn + 1}/{_turns}, racer {racer + 1}");
                    var stopwatch = Stopwatch.StartNew();
                    for (int i = 0; i < _count; i++)
                    {
                        _racers[racer]();
                        GC.Collect();
                    }
                    _times[racer][turn] = stopwatch.ElapsedTicks;
                }
            }
            return Task.CompletedTask;
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

            if (times.Length > 5)
                times = times.Except(new[] { times.Min(), times.Max() }).ToArray();

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
