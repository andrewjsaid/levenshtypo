using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

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

internal class LevenshtrieSearchResultComparer<T> : IEqualityComparer<LevenshtrieSearchResult<T>>
{
    public static LevenshtrieSearchResultComparer<T> Instance { get; } = new();

    public bool Equals(LevenshtrieSearchResult<T> x, LevenshtrieSearchResult<T> y)
        => x.Distance.Equals(y.Distance)
        && ((x.Result is null && y.Result is null) || (x.Result is not null && y.Result is not null && x.Result.Equals(y.Result)));

    public int GetHashCode([DisallowNull] LevenshtrieSearchResult<T> obj)
    {
        var hashCode = new HashCode();
        hashCode.Add(obj.Distance);
        hashCode.Add(obj.Result);
        return hashCode.ToHashCode();
    }
}
