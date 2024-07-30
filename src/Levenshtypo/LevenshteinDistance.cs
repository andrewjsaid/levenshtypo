using System;
using System.Buffers;

namespace Levenshtypo
{

    public static class LevenshteinDistance
    {

        public static int CalculateCaseSensitive(ReadOnlySpan<char> a, ReadOnlySpan<char> b)
        {
            var distancesLength = b.Length + 1;

#if NET8_0_OR_GREATER
            const int MaxStackallocBytes = 16 * 4;

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
#else
            var d0 = ArrayPool<int>.Shared.Rent(distancesLength);
            var d1 = ArrayPool<int>.Shared.Rent(distancesLength);
            int[] dSwap;
#endif

            for (int i = 0; i < distancesLength; i++)
            {
                d0[i] = i;
            }

            for (int i = 0; i < a.Length; i++)
            {
                d1[0] = i + 1;

                for (int j = 0; j < b.Length; j++)
                {
                    var deletionCost = d0[j + 1] + 1;
                    var insertionCost = d1[j] + 1;
                    var substitutionCost = d0[j] + (a[i] == b[j] ? 0 : 1);
                    d1[j + 1] = Math.Min(Math.Min(deletionCost, insertionCost), substitutionCost);
                }

                dSwap = d0;
                d0 = d1;
                d1 = dSwap;
            }

#if NET8_0_OR_GREATER
            if (rentedArr != null)
            {
                ArrayPool<int>.Shared.Return(rentedArr);
            }
#else
            ArrayPool<int>.Shared.Return(d0);
            ArrayPool<int>.Shared.Return(d1);
#endif

            return d0[b.Length];
        }

        public static int CalculateCaseInsensitive(ReadOnlySpan<char> a, ReadOnlySpan<char> b)
        {
            var distancesLength = b.Length + 1;

#if NET8_0_OR_GREATER
            const int MaxStackallocBytes = 16 * 4;

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
#else
            var d0 = ArrayPool<int>.Shared.Rent(distancesLength);
            var d1 = ArrayPool<int>.Shared.Rent(distancesLength);
            int[] dSwap;
#endif

            for (int i = 0; i < distancesLength; i++)
            {
                d0[i] = i;
            }

            for (int i = 0; i < a.Length; i++)
            {
                d1[0] = i + 1;

                for (int j = 0; j < b.Length; j++)
                {
                    var deletionCost = d0[j + 1] + 1;
                    var insertionCost = d1[j] + 1;
                    var substitutionCost = d0[j] + (char.ToLower(a[i]) == char.ToLower(b[j]) ? 0 : 1);
                    d1[j + 1] = Math.Min(Math.Min(deletionCost, insertionCost), substitutionCost);
                }

                dSwap = d0;
                d0 = d1;
                d1 = dSwap;
            }

#if NET8_0_OR_GREATER
            if (rentedArr != null)
            {
                ArrayPool<int>.Shared.Return(rentedArr);
            }
#else
            ArrayPool<int>.Shared.Return(d0);
            ArrayPool<int>.Shared.Return(d1);
#endif

            return d0[b.Length];
        }

    }

}