using Mofemofies.Data;
using System.Diagnostics.CodeAnalysis;

namespace Mofemofies.Components;

public class CEveryGivenTime : IMofiDisplay, IResettable
{
    protected CancellationTokenSource? _cancellation;

    public TimeSpan Interval { get; set; }
    public Action? Execute { get; set; }

    void IResettable.Reset()
    {
        if (_cancellation is not null && !_cancellation.IsCancellationRequested)
        {
            _cancellation.Cancel();
        }
    }

    [MemberNotNull(nameof(_cancellation))]
    public virtual void Load(long frame)
    {
        _cancellation = new();
        _ = Task.Run(ExecuteAsync);
    }

    private async Task ExecuteAsync()
    {
        if (Execute is null)
        {
            return;
        }

        using PeriodicTimer timer = new(Interval);
        try
        {
            while (await timer.WaitForNextTickAsync(_cancellation!.Token))
            {
                Execute();
            }
        }
        catch (OperationCanceledException)
        {

        }
    }
}
