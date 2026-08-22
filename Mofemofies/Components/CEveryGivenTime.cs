using Mofemofies.Data;
using System.Diagnostics.CodeAnalysis;

namespace Mofemofies.Components;

public class CEveryGivenTime : IMofiDisplay, IResettable, IDisposable
{
    protected CancellationTokenSource? _cancellation;
    private bool _disposedValue;

    public TimeSpan Interval { get; set; }
    public Action? Executor { get; set; }

    void IResettable.Reset()
    {
        if (_cancellation is not null && !_cancellation.IsCancellationRequested)
        {
            _cancellation.Cancel();
        }

        Interval = default;
        Executor = null;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _cancellation?.Dispose();
            }

            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    [MemberNotNull(nameof(_cancellation))]
    public virtual void Load(long frame)
    {
        _cancellation = new();
        _ = Task.Run(ExecuteAsync);
    }

    private async Task ExecuteAsync()
    {
        if (Executor is null)
        {
            return;
        }

        using PeriodicTimer timer = new(Interval);
        try
        {
            while (await timer.WaitForNextTickAsync(_cancellation!.Token))
            {
                Executor();
            }
        }
        catch (OperationCanceledException)
        {

        }
    }
}
