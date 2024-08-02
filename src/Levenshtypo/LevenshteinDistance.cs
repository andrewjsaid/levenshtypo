using System;
using System.Buffers;

namespace Levenshtypo;

/// <summary>
/// Static calculator for Levenshtein distance.
/// </summary>
public static class LevenshteinDistance
{
    private const int MaxStackallocBytes = 48 * 4;

    public static int Calculate(ReadOnlySpan<char> a, ReadOnlySpan<char> b, bool ignoreCase = false, LevenshtypoMetric metric = LevenshtypoMetric.Levenshtein)
    {
        return metric switch
        {
            LevenshtypoMetric.Levenshtein => Levenshtein(a, b, ignoreCase),
            LevenshtypoMetric.RestrictedEdit => RestrictedEdit(a, b, ignoreCase),
            _ => throw new NotSupportedException(nameof(metric))
        };
    }

    /// <summary>
    /// Calculates the levenshtein distance between two strings.
    /// </summary>
    public static int Levenshtein(ReadOnlySpan<char> a, ReadOnlySpan<char> b, bool ignoreCase = false)
    {
        if (ignoreCase)
        {
            return Levenshtein<CaseInsensitive>(a, b);
        }
        else
        {
            return Levenshtein<CaseSensitive>(a, b);
        }
    }

    private static int Levenshtein<TCaseSensitivity>(ReadOnlySpan<char> a, ReadOnlySpan<char> b) where TCaseSensitivity : struct, ICaseSensitivity<TCaseSensitivity>
    {
        if (a.Length < b.Length)
        {
            // b should have the smaller length
            var tmp = a;
            a = b;
            b = tmp;
        }

        var distancesLength = b.Length + 1;

        int[]? rentedArr = null;

        scoped Span<int> d0;
        scoped Span<int> d1;
        scoped Span<int> dSwap;

        if (distancesLength < MaxStackallocBytes / 4 / 2)
        {
            d0 = stackalloc int[distancesLength];
            d1 = stackalloc int[distancesLength];
        }
        else
        {
            rentedArr = ArrayPool<int>.Shared.Rent(distancesLength * 2);
            d0 = rentedArr.AsSpan(0, distancesLength);
            d1 = rentedArr.AsSpan(distancesLength, distancesLength);
        }

        for (int i = 0; i < distancesLength; i++)
        {
            d0[i] = i;
        }

        for (int i = 0; i < a.Length; i++)
        {
            d1[0] = i + 1;

            var ai = a[i];

            for (int j = 0; j < b.Length; j++)
            {
                var cost = default(TCaseSensitivity).Equals(ai, b[j]) ? 0 : 1;
                var deletionCost = d0[j + 1] + 1;
                var insertionCost = d1[j] + 1;
                var substitutionCost = d0[j] + cost;
                d1[j + 1] = Math.Min(Math.Min(deletionCost, insertionCost), substitutionCost);
            }

            dSwap = d0;
            d0 = d1;
            d1 = dSwap;
        }

        if (rentedArr != null)
        {
            ArrayPool<int>.Shared.Return(rentedArr);
        }

        return d0[b.Length];
    }

    /// <summary>
    /// Calculates the Restricted Edit Distance (a.k.a. Optimal String Alignment Distance)
    /// between two strings.
    /// </summary>
    public static int RestrictedEdit(ReadOnlySpan<char> a, ReadOnlySpan<char> b, bool ignoreCase = false)
    {
        if (ignoreCase)
        {
            return RestrictedEdit<CaseInsensitive>(a, b);
        }
        else
        {
            return RestrictedEdit<CaseSensitive>(a, b);
        }
    }

    private static int RestrictedEdit<TCaseSensitivity>(ReadOnlySpan<char> a, ReadOnlySpan<char> b) where TCaseSensitivity : struct, ICaseSensitivity<TCaseSensitivity>
    {
        if (a.Length < b.Length)
        {
            // b should have the smaller length
            var tmp = a;
            a = b;
            b = tmp;
        }

        var distancesLength = b.Length + 1;

        int[]? rentedArr = null;

        scoped Span<int> d0;
        scoped Span<int> d1;
        scoped Span<int> dN1;
        scoped Span<int> dSwap;

        if (distancesLength < MaxStackallocBytes / 4 / 3)
        {
            d0 = stackalloc int[distancesLength];
            d1 = stackalloc int[distancesLength];
            dN1 = stackalloc int[distancesLength];
        }
        else
        {
            rentedArr = ArrayPool<int>.Shared.Rent(distancesLength * 3);
            d0 = rentedArr.AsSpan(0, distancesLength);
            d1 = rentedArr.AsSpan(distancesLength, distancesLength);
            dN1 = rentedArr.AsSpan(distancesLength * 2, distancesLength);
        }

        for (int i = 0; i < distancesLength; i++)
        {
            d0[i] = i;
        }

        for (int i = 0; i < a.Length; i++)
        {
            d1[0] = i + 1;

            var ai = a[i];

            for (int j = 0; j < b.Length; j++)
            {
                var cost = default(TCaseSensitivity).Equals(ai, b[j]) ? 0 : 1;
                var deletionCost = d0[j + 1] + 1;
                var insertionCost = d1[j] + 1;
                var substitutionCost = d0[j] + cost;
                var min = Math.Min(Math.Min(deletionCost, insertionCost), substitutionCost);

                if (i > 0 && j > 0
                        && default(TCaseSensitivity).Equals(a[i - 1], b[j - 0])
                        && default(TCaseSensitivity).Equals(a[i - 0], b[j - 1]))
                {
                    min = Math.Min(min, dN1[j - 1] + 1);
                }

                d1[j + 1] = min;
            }

            dSwap = dN1;
            dN1 = d0;
            d0 = d1;
            d1 = dSwap;
        }

        if (rentedArr != null)
        {
            ArrayPool<int>.Shared.Return(rentedArr);
        }

        return d0[b.Length];
    }

}