using System.Diagnostics.CodeAnalysis;

namespace Levenshtypo.Tests;

internal class LevenshtrieSearchResultEqualityComparer<T> : IEqualityComparer<LevenshtrieSearchResult<T>>
{
    public bool Equals(LevenshtrieSearchResult<T> x, LevenshtrieSearchResult<T> y)
        => x.Distance == y.Distance
        && (x.Result?.Equals(y.Result) ?? (y.Result is null));

    public int GetHashCode([DisallowNull] LevenshtrieSearchResult<T> obj)
    {
        var hashCode = new HashCode();
        hashCode.Add(obj.Distance);
        hashCode.Add(obj.Result);
        return hashCode.ToHashCode();
    }
}
