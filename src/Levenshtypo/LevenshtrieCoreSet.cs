using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace Levenshtypo;

internal interface ILevenshtrieCoreSet<T>
{
    bool IgnoreCase { get; }

    LevenshtrieSearchResult<T>[] Search<TSearchState>(TSearchState searcher)
        where TSearchState : ILevenshtomatonExecutionState<TSearchState>;

    IEnumerable<LevenshtrieSearchResult<T>> EnumerateSearch<TSearchState>(TSearchState searcher)
        where TSearchState : ILevenshtomatonExecutionState<TSearchState>;

    ref T GetOrAddRef(ReadOnlySpan<char> key, T value, out bool exists);

    bool ContainsKey(ReadOnlySpan<char> key);

    bool Contains(ReadOnlySpan<char> key, T value);

    LevenshtrieCoreSetCursor<T> GetValues(ReadOnlySpan<char> key);

    bool Add(ReadOnlySpan<char> key, T value);

    bool RemoveAll(ReadOnlySpan<char> key);

    bool Remove(ReadOnlySpan<char> key, T value);
}

internal sealed class LevenshtrieCoreSet<T, TCaseSensitivity>
    : LevenshtrieCore<T, TCaseSensitivity, LevenshtrieCoreSetCursor<T>>,
    ILevenshtrieCoreSet<T>
    where TCaseSensitivity : struct, ICaseSensitivity<TCaseSensitivity>
{
    /// <summary>
    /// This is the (average) maximum length of a linked list. Above this we
    /// start adding more buckets. Decreasing this to 1 yields similar performance
    /// to HashSet but also increases memory usage.
    /// This library optimizes tries for searching so we are erring towards
    /// less memory usage and slower indexing.
    /// In the future we may choose to make this configurable.
    /// </summary>
    private const int LinkedListThreshold = 25;
    private const int LargeResultFlag = unchecked((int)0x8000_0000); // high bit

    private LevenshtrieCoreSetResult<T>[] _results = [];
    private int _resultsSize;

    private LevenshtrieCoreSetLargeResult[] _largeResults = [];
    private int _largeResultsSize;

    private readonly IEqualityComparer<T> _resultEqualityComparer;

    private LevenshtrieCoreSet(LevenshtrieCoreEntry root, IEqualityComparer<T>? resultEqualityComparer) : base(root)
    {
        _resultEqualityComparer = resultEqualityComparer ?? EqualityComparer<T>.Default;
    }

    internal static ILevenshtrieCoreSet<T> Create(
        IEnumerable<KeyValuePair<string, T>> source,
        IEqualityComparer<T>? resultEqualityComparer)
    {
        var root = new LevenshtrieCoreEntry
        {
            EntryValue = Rune.ReplacementChar,
            ResultIndex = NoIndex,
            FirstChildEntryIndex = NoIndex,
            TailDataIndex = NoIndex,
            NextSiblingEntryIndex = NoIndex,
            TailDataLength = 0
        };

        var trie = new LevenshtrieCoreSet<T, TCaseSensitivity>(root, resultEqualityComparer);

        foreach (var (key, value) in source)
        {
            trie.Add(key, value);
        }

        return trie;
    }

    public bool IgnoreCase => typeof(TCaseSensitivity) == typeof(CaseInsensitive);

    /// <summary>
    /// Traverses a linked list of result entries starting at <paramref name="index"/>,
    /// searching for a result that matches the specified <paramref name="hash"/> and <paramref name="value"/>.
    /// </summary>
    /// <param name="index">
    /// The index of the first entry in the result chain. This acts as the head of the linked list to traverse.
    /// </param>
    /// <param name="hash">
    /// The precomputed hash of the value being searched. Used for fast preliminary filtering.
    /// </param>
    /// <param name="value">
    /// The value to find within the result chain.
    /// </param>
    /// <param name="matchOrTailIndex">
    /// When the method returns:
    /// - If a match is found, this is set to the index of the matching result entry.
    /// - If no match is found, this is set to the index of the final entry in the chain.
    /// </param>
    /// <returns>
    /// <c>true</c> if an entry matching the <paramref name="hash"/> and <paramref name="value"/> is found;
    /// otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// This method does not mutate the chain, but provides the location of either the match or the insertion point.
    /// If the result is <c>false</c>, you may append a new result entry by updating the <c>NextResultIndex</c>
    /// of the node at <paramref name="matchOrTailIndex"/>.
    /// </remarks>

    private bool FollowFindIndex(int index, uint hashCode, T value, out int matchOrTailIndex, out int chainLength)
    {
        chainLength = 0;

        matchOrTailIndex = NoIndex;

        while (index is not NoIndex)
        {
            chainLength++;

            ref var entry = ref _results[index];

            if (entry.HashCode == hashCode && _resultEqualityComparer.Equals(entry.Value, value))
            {
                matchOrTailIndex = index;
                return true;
            }

            matchOrTailIndex = index;
            index = entry.NextResultIndex;
        }

        return false;
    }

    public ref T GetOrAddRef(ReadOnlySpan<char> key, T value, out bool exists)
    {
        ref var entry = ref GetOrAddEntryRef(key);

        var hash = value is null ? 0u : unchecked((uint)_resultEqualityComparer.GetHashCode(value));

        ref var headIndex = ref entry.ResultIndex;

        if (entry.ResultIndex is NoIndex)
        {
            // This is the "easy" case as we just insert a single entry
            var newSingleResultIndex = entry.ResultIndex = TakeResultSlot();
            exists = false;

            ref var singleResult = ref _results[newSingleResultIndex];
            singleResult = new LevenshtrieCoreSetResult<T>
            {
                Value = value,
                HashCode = hash,
                NextResultIndex = NoIndex
            };
            return ref singleResult.Value;
        }

        ref LevenshtrieCoreSetLargeResult largeResult = ref Unsafe.NullRef<LevenshtrieCoreSetLargeResult>();

        if (IsLargeIndex(headIndex, out var largeIndex))
        {
            largeResult = ref _largeResults[largeIndex];
            var bucketHeads = largeResult.BucketHeads;
            headIndex = ref bucketHeads[hash % bucketHeads.Length];
        }

        if (FollowFindIndex(headIndex, hash, value, out var matchOrTailIndex, out var chainLength))
        {
            exists = true;
            return ref _results[matchOrTailIndex].Value;
        }

        exists = false;

        // We need to add it to an existing linked list
        var newResultIndex = TakeResultSlot();
        ref var result = ref _results[newResultIndex];
        result = new LevenshtrieCoreSetResult<T>
        {
            Value = value,
            HashCode = hash,
            NextResultIndex = NoIndex
        };

        if (matchOrTailIndex is NoIndex)
        {
            headIndex = newResultIndex;
        }
        else
        {
            _results[matchOrTailIndex].NextResultIndex = newResultIndex;
        }


        // Decide whether we must re-bucket 
        Span<int> stackSpace = stackalloc int[1];
        scoped Span<int> prevBuckets = Span<int>.Empty;

        if (!Unsafe.IsNullRef(ref largeResult))
        {
            largeResult.Count++;

            var growBuckets = largeResult.Count > largeResult.BucketHeads.Length * LinkedListThreshold;
            if (growBuckets)
            {
                prevBuckets = largeResult.BucketHeads;
                largeResult.BucketHeads = new int[HashHelpers.ExpandPrime(largeResult.BucketHeads.Length)];
            }
        }
        else
        {
            if (chainLength > LinkedListThreshold)
            {
                // We can remove this allocation if it shows to be problematic
                prevBuckets = stackSpace;
                prevBuckets[0] = entry.ResultIndex;

                // We will be bucketing this entry for the first time
                largeIndex = TakeLargeResultSlot();
                largeResult = ref _largeResults[largeIndex];
                largeResult = new LevenshtrieCoreSetLargeResult
                {
                    BucketHeads = new int[HashHelpers.ExpandPrime(chainLength)],
                    Count = chainLength + 1
                };
                entry.ResultIndex = LargeResultFlag | largeIndex;
            }
        }

        // Here we actually re-bucket
        if (prevBuckets.Length > 0)
        {
            var bucketHeads = largeResult.BucketHeads;
            Array.Fill(bucketHeads, NoIndex);

            foreach (var prevBucketHead in prevBuckets)
            {
                // At this point the new buckets array has already been assigned.
                var bucketingResultIndex = prevBucketHead;
                while (bucketingResultIndex is not NoIndex)
                {
                    var currentResultIndex = bucketingResultIndex;

                    ref var bucketingResult = ref _results[currentResultIndex];
                    bucketingResultIndex = bucketingResult.NextResultIndex;

                    var newBucketIndex = bucketingResult.HashCode % bucketHeads.Length;
                    bucketingResult.NextResultIndex = bucketHeads[newBucketIndex];
                    bucketHeads[newBucketIndex] = currentResultIndex;
                }
            }
        }

        return ref result.Value;
    }

    public bool ContainsKey(ReadOnlySpan<char> key)
    {
        ref var entry = ref GetEntryRef(key);

        return !Unsafe.IsNullRef(ref entry) && entry.ResultIndex is not NoIndex;
    }

    public bool Contains(ReadOnlySpan<char> key, T value)
    {
        ref var entry = ref GetEntryRef(key);

        if (Unsafe.IsNullRef(ref entry) || entry.ResultIndex is NoIndex)
        {
            return false;
        }

        var hash = value is null ? 0u : unchecked((uint)_resultEqualityComparer.GetHashCode(value));

        var headIndex = entry.ResultIndex;

        if (headIndex is NoIndex)
        {
            return false;
        }

        if (IsLargeIndex(headIndex, out var largeIndex))
        {
            var bucketHeads = _largeResults[largeIndex].BucketHeads;
            headIndex = bucketHeads[hash % bucketHeads.Length];
        }

        return FollowFindIndex(headIndex, hash, value, out var foundIndex, out _);
    }

    public LevenshtrieCoreSetCursor<T> GetValues(ReadOnlySpan<char> key)
    {
        ref var entry = ref GetEntryRef(key);

        var resultIndex = Unsafe.IsNullRef(ref entry)
            ? NoIndex
            : entry.ResultIndex;

        return CreateCursor(resultIndex);
    }

    public bool Add(ReadOnlySpan<char> key, T value)
    {
        GetOrAddRef(key, value, out var exists);
        return !exists;
    }

    public bool RemoveAll(ReadOnlySpan<char> key)
    {
        ref var entry = ref GetEntryRef(key);

        if (Unsafe.IsNullRef(ref entry) || entry.ResultIndex is NoIndex)
        {
            return false;
        }

        Span<int> stackSpace = stackalloc int[1];
        scoped Span<int> prevBuckets = Span<int>.Empty;

        if (IsLargeIndex(entry.ResultIndex, out var largeIndex))
        {
            ref var largeResult = ref _largeResults[largeIndex];
            prevBuckets = largeResult.BucketHeads;
            largeResult.BucketHeads = [];
            largeResult.Count = 0;
        }
        else
        {
            prevBuckets = stackSpace;
            stackSpace[0] = entry.ResultIndex;
        }

        entry.ResultIndex = NoIndex;

        bool any = false;

        if (prevBuckets.Length > 0)
        {
            foreach (var headIndex in prevBuckets)
            {
                // At this point the new buckets array has already been assigned.
                var nextResultIndex = headIndex;
                while (nextResultIndex is not NoIndex)
                {
                    ref var nextResult = ref _results[nextResultIndex];
                    nextResultIndex = nextResult.NextResultIndex;
                    nextResult = default!;
                    any = true;
                }
            }
        }

        return any;
    }

    public bool Remove(ReadOnlySpan<char> key, T value)
    {
        ref var entry = ref GetEntryRef(key);

        if (Unsafe.IsNullRef(ref entry) || entry.ResultIndex is NoIndex)
        {
            return false;
        }

        var hash = value is null ? 0u : unchecked((uint)_resultEqualityComparer.GetHashCode(value));
        ref int headPtr = ref Unsafe.NullRef<int>();

        ref LevenshtrieCoreSetLargeResult largeResult = ref Unsafe.NullRef<LevenshtrieCoreSetLargeResult>();

        if (IsLargeIndex(entry.ResultIndex, out var largeIndex))
        {
            largeResult = ref _largeResults[largeIndex];
            if (largeResult.Count == 0)
            {
                return false;
            }
            
            var bucketIndex = hash % largeResult.BucketHeads.Length;
            headPtr = ref largeResult.BucketHeads[bucketIndex];
        }
        else
        {
            headPtr = ref entry.ResultIndex;
        }

        if (!Unsafe.IsNullRef(ref headPtr))
        {
            // At this point the new buckets array has already been assigned.
            var nextResultIndex = headPtr;
            while (nextResultIndex is not NoIndex)
            {
                ref var nextResult = ref _results[nextResultIndex];
                if (nextResult.HashCode == hash && _resultEqualityComparer.Equals(nextResult.Value, value))
                {
                    if (!Unsafe.IsNullRef(ref largeResult))
                    {
                        largeResult.Count--;
                    }

                    headPtr = nextResult.NextResultIndex;
                    nextResult = default!;
                    return true;
                }
                headPtr = ref nextResult.NextResultIndex;
                nextResultIndex = nextResult.NextResultIndex;
            }
        }

        return false;
    }

    protected override LevenshtrieCoreSetCursor<T> CreateCursor(int resultIndex)
    {
        if (resultIndex is not NoIndex && IsLargeIndex(resultIndex, out var largeIndex))
        {
            // We avoid the buckets since we want to loop through all entries
            return new(_results, _largeResults[largeIndex].BucketHeads);
        }
        else
        {
            return new(_results, resultIndex);
        }
    }

    private int TakeResultSlot()
    {
        var spacesRequired = 1;
        while (_results.Length - _resultsSize < spacesRequired)
        {
            Array.Resize(ref _results, NextArraySize(_results.Length));
        }
        return _resultsSize++;
    }

    private int TakeLargeResultSlot()
    {
        var spacesRequired = 1;
        while (_largeResults.Length - _largeResultsSize < spacesRequired)
        {
            Array.Resize(ref _largeResults, NextArraySize(_largeResults.Length));
        }
        return _largeResultsSize++;
    }

    private static bool IsLargeIndex(int index, out int largeIndex)
    {
        if ((index & LargeResultFlag) is not 0)
        {
            largeIndex = index & ~LargeResultFlag;
            return true;
        }

        largeIndex = default;
        return false;
    }
}

[DebuggerDisplay("{Value}")]
internal struct LevenshtrieCoreSetResult<T>
{
    /// <summary>
    /// The value stored at this location.
    /// </summary>
    public T Value;

    /// <summary>
    /// The hash of <see cref="Value"/>.
    /// </summary>
    public uint HashCode;

    /// <summary>
    /// When the results have been bucketed,
    /// this points to the next result in the bucketed
    /// linked list.
    /// <para/>
    /// When the results have not been bucketed
    /// this points to the next result in the full
    /// set of entries.
    /// </summary>
    public int NextResultIndex;
}

internal struct LevenshtrieCoreSetLargeResult
{
    public int[] BucketHeads;
    public int Count;
}

internal struct LevenshtrieCoreSetCursor<T> : ILevenshtrieCursor<T>
{
    private const int NoIndex = -1;

    private LevenshtrieCoreSetResult<T>[] _results;
    private int[] _buckets;
    private int _index;
    private int _nextBucketIndex;

    public LevenshtrieCoreSetCursor(LevenshtrieCoreSetResult<T>[] results, int index)
    {
        _results = results;
        _index = index;
        _buckets = [];
        _nextBucketIndex = 0;
    }

    public LevenshtrieCoreSetCursor(
        LevenshtrieCoreSetResult<T>[] results,
        int[] buckets)
    {
        _results = results;
        _index = NoIndex;
        _buckets = buckets;
        _nextBucketIndex = 0;
    }

    public bool MoveNext([MaybeNullWhen(false)] out T value)
    {
        var index = _index;

        while (index is NoIndex)
        {
            if (_nextBucketIndex < _buckets.Length)
            {
                index = _buckets[_nextBucketIndex++];
            }
            else
            {
                value = default;
                return false;
            }
        }

        value = _results[index].Value;
        _index = _results[index].NextResultIndex;
        return true;
    }
}
