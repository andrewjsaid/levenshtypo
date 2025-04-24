using System.Diagnostics;

namespace Levenshtypo;

[DebuggerDisplay("{Result} [{Distance}]")]
public struct LevenshtrieSearchResult<T>
{
    internal LevenshtrieSearchResult(int distance, T result)
    {
        Distance = distance;
        Result = result;
    }

    public int Distance { get; }

    public T Result { get; }

    public override readonly string ToString() => Result?.ToString() ?? string.Empty;
}
