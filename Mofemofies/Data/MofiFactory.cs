namespace Mofemofies.Data;

public abstract class MofiFactory<TSelf, TTarget>
    where TSelf : MofiFactory<TSelf, TTarget>, new()
    where TTarget : class
{
    protected Action<TTarget>? _creator;

    public virtual TSelf Inherit(Action<TTarget> modifier)
        => new() { _creator = _creator is null ? modifier : _creator + modifier };

    /// <summary>
    /// When implementing this instantiation method, it is recommended that
    /// you should hide the constructor in order to use factories to create instances.
    /// </summary>
    /// <returns></returns>
    protected abstract TTarget Instantiate();

    public virtual TTarget Create()
    {
        var scene = Instantiate();
        _creator?.Invoke(scene);
        return scene;
    }
}
