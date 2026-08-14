using Mofemofies.Data;
using SkiaSharp;

namespace Mofemofies;

// Scenes only know channels.
public sealed class MofiScene : IMofiDisplay, ICancelable, IDisposable
{
    public long GlobalStartFrame { get; private set; }
    public ExLong Length { get; private set; }
    public long LocalFrame { get; private set; }
    public bool IsFinished { get; set; }
    public List<MofiChannel> Channels { get; } = [];

    public void Dispose()
    {
        Channels.ForEach(channel => ((IResettable)channel).Reset());
        Channels.Clear();
    }

    public IEnumerable<MofiChannel> GetChannels(string? name)
        => Channels.Where(channel => channel.Name is null
        ? name is null
        : channel.Name.Equals(name));

    public void RemoveChannels(string? name)
        => Channels.RemoveAll(channel => channel.Name is null
        ? name is null
        : channel.Name.Equals(name));

    void IMofiDisplay.Load(long frame)
    {
        IsFinished = false;
        GlobalStartFrame = frame;
        LocalFrame = 0;

        Channels.ForEach(channel => channel.CheckLength());
        Length = Channels.Any(channel => channel.Length!.Value.IsPositiveInfinity)
            ? ExLong.PositiveInfinity
            : Channels.Max(channel => channel.Length!.Value);
    }

    void IMofiDisplay.Update(DisplayCallStack sender, long frame)
    {
        LocalFrame = frame - GlobalStartFrame;
        if (LocalFrame >= Length)
        {
            IsFinished = true;
            return;
        }

        sender.Push(this);
        foreach (var channel in Channels)
        {
            if (channel.QueryEvent(LocalFrame, out var currentEvent))
            {
                ((IMofiDisplay)currentEvent).Load(LocalFrame);
            }

            if (currentEvent is not null && !currentEvent.IsFinished)
            {
                ((IMofiDisplay)currentEvent).Update(sender, LocalFrame);
            }
        }
    }

    void IMofiDisplay.Render(SKCanvas canvas)
    {
        foreach (var channel in Channels)
        {
            _ = channel.QueryEvent(LocalFrame, out var currentEvent);
            if (currentEvent is not null && !currentEvent.IsFinished)
            {
                ((IMofiDisplay)currentEvent).Render(canvas);
            }
        }
    }
}

public sealed class MofiSceneFactory : MofiFactory<MofiSceneFactory, MofiScene>
{
    public MofiSceneFactory()
    {

    }

    public MofiSceneFactory(Action<MofiScene> creator)
    {
        _creator = creator;
    }
}
