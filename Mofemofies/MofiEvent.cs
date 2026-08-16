using Mofemofies.Data;
using SkiaSharp;

namespace Mofemofies;

public sealed class MofiEvent : MofiObject<IMofiDisplay>, IMofiDisplay, ICancelable, IDisposable
{
    internal Func<long, bool>? _finishPredicate;

    public long StartFrame { get; internal set; }
    public ExLong Length { get; internal set; }
    public double? Progress { get; internal set; }
    public bool IsFinished { get; set; }

    internal MofiEvent()
    {

    }

    public void Dispose() => ClearComponents();

    void IMofiDisplay.Load(long frame)
    {
        IsFinished = false;
        foreach ((_, var component) in _components)
        {
            component.Load(frame);
        }
    }

    void IMofiDisplay.Update(DisplayCallStack sender, long frame)
    {
        if (_finishPredicate!.Invoke(frame))
        {
            IsFinished = true;
            return;
        }

        sender.Push(this);
        foreach ((_, var component) in _components)
        {
            component.Update(sender, frame);
        }
    }

    void IMofiDisplay.Render(SKCanvas canvas)
    {
        foreach ((_, var component) in _components)
        {
            component.Render(canvas);
        }
    }
}

/// <summary>
/// Treated as an integer interval [<see cref="MofiEventFactory.StartFrame"/>, <see cref="MofiEventFactory.EndFrame"/>).
/// </summary>
public sealed class MofiEventFactory : MofiFactory<MofiEventFactory, MofiEvent>
{
    public static readonly MofiEventFactory MinValue = new(long.MinValue);
    public static readonly MofiEventFactory MaxValue = new(long.MaxValue);

    public long StartFrame { get; set; }
    public ExLong EndFrame { get; set; }

    private MofiEventFactory(long startFrame, ExLong endFrame, Action<MofiEvent>? creator)
    {
        if (startFrame >= endFrame)
        {
            throw new ArgumentException($"{nameof(startFrame)} must be less than {nameof(endFrame)}.");
        }

        StartFrame = startFrame;
        EndFrame = endFrame;

        _creator = creator;
    }

    public MofiEventFactory(long startFrame, long endFrame, Action<MofiEvent>? creator = null)
        : this(startFrame, (ExLong)endFrame, creator)
    {

    }

    public MofiEventFactory(long startFrame, Action<MofiEvent>? creator = null)
        : this(startFrame, ExLong.PositiveInfinity, creator)
    {

    }

    public MofiEventFactory(long startFrame, long endFrame, MofiEventFactory source)
        : this(startFrame, endFrame, source._creator)
    {

    }

    public MofiEventFactory(long startFrame, MofiEventFactory source)
        : this(startFrame, ExLong.PositiveInfinity, source._creator)
    {

    }

    public MofiEventFactory()
        : this(default, ExLong.PositiveInfinity, null)
    {

    }

    public static MofiEventFactory CreateInstant(long frame) => new(frame, frame + 1);

    public void SetCreator(Action<MofiEvent> creator) => _creator = creator;

    protected override MofiEvent Instantiate() => new();

    public override MofiEvent Create()
    {
        var newEvent = base.Create();
        newEvent._finishPredicate = frame => frame >= EndFrame;
        newEvent.Length = EndFrame.IsFinite ? (long)EndFrame - StartFrame - 1 : ExLong.PositiveInfinity;
        return newEvent;
    }
}
