using SkiaSharp;

namespace Mofemofies;

/// <summary>
/// Implementing this interface means you can push it onto the <see cref="DisplayCallStack"/>
/// to wait for a possible cancellation.
/// </summary>
public interface ICancelable
{
    bool IsFinished { get; set; }
}

public sealed class DisplayCallStack
{
    private readonly Stack<ICancelable> _stack = [];

    public void Push(ICancelable sender) => _stack.Push(sender);

    public void Pop<T>() where T : ICancelable
    {
        ICancelable caller;
        do
        {
            caller = _stack.Pop();
        }
        while (caller is not T);
        caller.IsFinished = true;
    }
}

public interface IMofiDisplay
{
    virtual void Load(long frame) { }
    virtual void Update(DisplayCallStack sender, long frame) { }
    virtual void Render(SKCanvas canvas) { }
}
