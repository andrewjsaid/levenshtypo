using System.Diagnostics.CodeAnalysis;

namespace Levenshtypo.Tests;

internal class LevenshtrieSearchResultEqualityComparer<T> : IEqualityComparer<LevenshtrieSearchResult<T>>
{
    public bool Equals(LevenshtrieSearchResult<T> x, LevenshtrieSearchResult<T> y)
        => x.Distance == y.Distance
        && (x.Result?.Equals(y.Result) ?? (y.Result is null))
        && x.Kind.Equals(y.Kind)
        && (!x.TryGetPrefixSearchMetadata(out var xPrefixMetadata)
        || !y.TryGetPrefixSearchMetadata(out var yPrefixMetadata) 
            || (xPrefixMetadata.PrefixLength.Equals(xPrefixMetadata.PrefixLength) && xPrefixMetadata.SuffixLength.Equals(xPrefixMetadata.SuffixLength)));

    public int GetHashCode([DisallowNull] LevenshtrieSearchResult<T> obj)
    {
        var hashCode = new HashCode();
        hashCode.Add(obj.Distance);
        hashCode.Add(obj.Result);
        hashCode.Add(obj.Kind);
        if (obj.TryGetPrefixSearchMetadata(out var metadata))
        {
            hashCode.Add(metadata.PrefixLength);
            hashCode.Add(metadata.SuffixLength);
        }
        return hashCode.ToHashCode();
    }
}
