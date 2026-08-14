using Fractions;
using System.Diagnostics;

namespace Mofemofies;

public sealed class ObstructiveTimer
{
    private readonly Stopwatch _timer = new();
    private long _nextFrameIndex;
    private long _startTicks;

    public Fraction FPS { get; set; }
    public long NextTicks => _startTicks + (_nextFrameIndex * Stopwatch.Frequency / FPS).ToInt64();

    public void Restart()
    {
        _timer.Restart();
        _nextFrameIndex = 1;
        _startTicks = _timer.ElapsedTicks;
    }

    public void Proceed() => _nextFrameIndex++;

    public void Wait()
    {
        while (_timer.ElapsedTicks < NextTicks)
        {
            var remainingTicks = NextTicks - _timer.ElapsedTicks;
            var remainingMs = 1000.0 * remainingTicks / Stopwatch.Frequency;

            if (remainingMs > 2)
            {
                Thread.Sleep((int)(remainingMs - 1.5));
            }
            else
            {
                Thread.SpinWait(10);
            }
        }
    }

    public void Stop() => _timer.Stop();
}
