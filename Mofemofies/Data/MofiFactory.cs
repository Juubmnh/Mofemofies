namespace Mofemofies.Data;

public class MofiFactory<TSelf, TTarget>
    where TSelf : MofiFactory<TSelf, TTarget>, new()
    where TTarget : class, new()
{
    protected Action<TTarget>? _creator;

    public virtual TSelf Inherit(Action<TTarget> modifier)
        => new() { _creator = _creator is null ? modifier : _creator + modifier };

    public virtual TTarget Create()
    {
        TTarget scene = new();
        _creator?.Invoke(scene);
        return scene;
    }
}
