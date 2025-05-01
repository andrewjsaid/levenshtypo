namespace Levenshtypo;

/// <summary>
/// Purely a wrapper to be able to implement the same interface multiple times.
/// But using a different wrapper to reference the correct one.
/// </summary>
internal readonly struct SearchByPrefixWrapper<TWrapped>(TWrapped wrapped)
{
    public TWrapped Wrapped { get; } = wrapped;
}
