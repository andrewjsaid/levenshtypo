using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;

namespace Levenshtypo;

/// <summary>
/// Provides static methods for computing Levenshtein-style edit distances between strings or spans,
/// with support for Unicode-awareness, configurable case sensitivity, and multiple distance metrics.
/// </summary>
/// <remarks>
/// All methods are allocation-aware and make use of stackalloc and array pooling internally
/// to reduce GC pressure. These routines operate over <see cref="ReadOnlySpan{Char}"/> inputs
/// and support both char-based and rune-based traversal, depending on the presence of surrogate pairs.
/// </remarks>
public static class LevenshteinDistance
{
    private const int MaxStackallocBytes = 48 * 4;

    /// <summary>
    /// Computes the edit distance between two strings using the specified distance metric.
    /// </summary>
    /// <param name="a">The first string to compare.</param>
    /// <param name="b">The second string to compare.</param>
    /// <param name="ignoreCase">
    /// When <c>true</c>, performs case-insensitive comparisons using invariant culture semantics.
    /// </param>
    /// <param name="metric">
    /// The edit distance algorithm to use. See <see cref="LevenshtypoMetric"/> for supported options.
    /// </param>
    /// <returns>
    /// The minimum number of edits required to transform <paramref name="a"/> into <paramref name="b"/>.
    /// </returns>
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
    /// Computes the classic Levenshtein distance between two strings, optionally ignoring case.
    /// </summary>
    /// <param name="a">The source string.</param>
    /// <param name="b">The target string.</param>
    /// <param name="ignoreCase">Whether to perform a case-insensitive comparison.</param>
    /// <returns>The Levenshtein distance between the two inputs.</returns>
    public static int Levenshtein(ReadOnlySpan<char> a, ReadOnlySpan<char> b, bool ignoreCase = false)
    {
        if (!ContainsSurrogate(a) && !ContainsSurrogate(b))
        {
            if (ignoreCase)
            {
                return LevenshteinT<char, CaseInsensitive>(a, b);
            }
            else
            {
                return LevenshteinT<char, CaseSensitive>(a, b);
            }
        }
        else
        {
            if (ignoreCase)
            {
                return LevenshteinRune<CaseInsensitive>(a, b);
            }
            else
            {
                return LevenshteinRune<CaseSensitive>(a, b);
            }
        }
    }

    private static int LevenshteinRune<TCaseSensitivity>(ReadOnlySpan<char> aChar, ReadOnlySpan<char> bChar) where TCaseSensitivity : struct, ICaseSensitivity<TCaseSensitivity>
    {
        Rune[]? rentedRuneArr = null;

        scoped Span<Rune> a;
        scoped Span<Rune> b;

        // The number of runes will always be equal or less than the number of chars
        if ((aChar.Length + bChar.Length) < MaxStackallocBytes / 4 / 2)
        {
            a = stackalloc Rune[aChar.Length];
            b = stackalloc Rune[bChar.Length];
        }
        else
        {
            rentedRuneArr = ArrayPool<Rune>.Shared.Rent(aChar.Length + bChar.Length);
            a = rentedRuneArr.AsSpan(0, aChar.Length);
            b = rentedRuneArr.AsSpan(aChar.Length, bChar.Length);
        }
        
        int runeWriteIndex = 0;
        foreach (var rune in aChar.EnumerateRunes())
        {
            a[runeWriteIndex++] = rune;
        }

        a = a[..runeWriteIndex];

        runeWriteIndex = 0;
        foreach (var rune in bChar.EnumerateRunes())
        {
            b[runeWriteIndex++] = rune;
        }

        b = b[..runeWriteIndex];

        var result = LevenshteinT<Rune, TCaseSensitivity>(a, b);

        if (rentedRuneArr is not null)
        {
            ArrayPool<Rune>.Shared.Return(rentedRuneArr);
        }

        return result;
    }

    private static int LevenshteinT<T, TCaseSensitivity>(ReadOnlySpan<T> a, ReadOnlySpan<T> b)
        where TCaseSensitivity : struct, ICaseSensitivity<TCaseSensitivity>
        where T : struct
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

        for (int i = 0; i < d0.Length; i++)
        {
            d0[i] = i;
        }

        for (int i = 0; i < a.Length; i++)
        {
            d1[0] = i + 1;

            var ai = a[i];

            for (int j = 0; j < b.Length; j++)
            {
                var cost = CharEquals<T, TCaseSensitivity>(ai, b[j]) ? 0 : 1;
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
    /// Computes the Restricted Edit Distance (Optimal String Alignment) between two strings,
    /// optionally ignoring case.
    /// </summary>
    /// <param name="a">The source string.</param>
    /// <param name="b">The target string.</param>
    /// <param name="ignoreCase">Whether to perform a case-insensitive comparison.</param>
    /// <returns>The restricted edit distance between the two inputs.</returns>
    public static int RestrictedEdit(ReadOnlySpan<char> a, ReadOnlySpan<char> b, bool ignoreCase = false)
    {
        if (!ContainsSurrogate(a) && !ContainsSurrogate(b))
        {
            if (ignoreCase)
            {
                return RestrictedEditT<char, CaseInsensitive>(a, b);
            }
            else
            {
                return RestrictedEditT<char, CaseSensitive>(a, b);
            }
        }
        else
        {
            if (ignoreCase)
            {
                return RestrictedEditRune<CaseInsensitive>(a, b);
            }
            else
            {
                return RestrictedEditRune<CaseSensitive>(a, b);
            }
        }
    }

    private static int RestrictedEditRune<TCaseSensitivity>(ReadOnlySpan<char> aChar, ReadOnlySpan<char> bChar) where TCaseSensitivity : struct, ICaseSensitivity<TCaseSensitivity>
    {
        Rune[]? rentedRuneArr = null;

        scoped Span<Rune> a;
        scoped Span<Rune> b;

        // The number of runes will always be equal or less than the number of chars
        if ((aChar.Length + bChar.Length) < MaxStackallocBytes / 4 / 2)
        {
            a = stackalloc Rune[aChar.Length];
            b = stackalloc Rune[bChar.Length];
        }
        else
        {
            rentedRuneArr = ArrayPool<Rune>.Shared.Rent(aChar.Length + bChar.Length);
            a = rentedRuneArr.AsSpan(0, aChar.Length);
            b = rentedRuneArr.AsSpan(aChar.Length, bChar.Length);
        }
        
        int runeWriteIndex = 0;
        foreach (var rune in aChar.EnumerateRunes())
        {
            a[runeWriteIndex++] = rune;
        }

        a = a[..runeWriteIndex];

        runeWriteIndex = 0;
        foreach (var rune in bChar.EnumerateRunes())
        {
            b[runeWriteIndex++] = rune;
        }

        b = b[..runeWriteIndex];

        var result = RestrictedEditT<Rune, TCaseSensitivity>(a, b);

        if (rentedRuneArr is not null)
        {
            ArrayPool<Rune>.Shared.Return(rentedRuneArr);
        }

        return result;
    }

    private static int RestrictedEditT<T, TCaseSensitivity>(ReadOnlySpan<T> a, ReadOnlySpan<T> b)
        where TCaseSensitivity : struct, ICaseSensitivity<TCaseSensitivity>
        where T : struct
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
                var cost = CharEquals<T, TCaseSensitivity>(ai, b[j]) ? 0 : 1;
                var deletionCost = d0[j + 1] + 1;
                var insertionCost = d1[j] + 1;
                var substitutionCost = d0[j] + cost;
                var min = Math.Min(Math.Min(deletionCost, insertionCost), substitutionCost);

                if (i > 0 && j > 0
                    && CharEquals<T, TCaseSensitivity>(a[i - 1], b[j - 0])
                    && CharEquals<T, TCaseSensitivity>(a[i - 0], b[j - 1]))
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

    /// <summary>
    /// Computes Levenshtein distance using a generic value comparer for either char or Rune types.
    /// This method is used internally to unify Unicode-aware and ASCII-based execution paths.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool CharEquals<T, TCaseSensitivity>(T a, T b)
        where TCaseSensitivity : struct, ICaseSensitivity<TCaseSensitivity>
        where T : struct
    {
        if (typeof(T) == typeof(Rune))
        {
            return default(TCaseSensitivity).Equals(Unsafe.As<T, Rune>(ref a), Unsafe.As<T, Rune>(ref b));
        }
        else if (typeof(T) == typeof(char))
        {
            return default(TCaseSensitivity).Equals(Unsafe.As<T, char>(ref a), Unsafe.As<T, char>(ref b));
        }
        else
        {
            throw new NotSupportedException();
        }
    }

    private static bool ContainsSurrogate(ReadOnlySpan<char> text)
    {
        foreach (var c in text)
        {
            if (char.IsSurrogate(c))
            {
                return true;
            }
        }

        return false;
    }
}