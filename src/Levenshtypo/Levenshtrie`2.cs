using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace Levenshtypo;

internal sealed class Levenshtrie<T, TCaseSensitivity> :
    Levenshtrie<T>, ILevenshtomatonExecutor<LevenshtrieSearchResult<T>[]> where TCaseSensitivity : struct, ICaseSensitivity<TCaseSensitivity>
{
    private const int NoIndex = -1;

    private Entry[] _entries;
    private int _entriesSize;

    private T[] _results;
    private int _resultsSize;

    private char[] _tailData;
    private int _tailDataSize;

    private Levenshtrie(Entry[] entries, T[] results, char[] tailData)
    {
        _entries = entries;
        _entriesSize = _entries.Length;
        _results = results;
        _resultsSize = results.Length;
        _tailData = tailData;
        _tailDataSize = tailData.Length;
    }

    private protected override bool IgnoreCase => typeof(TCaseSensitivity) == typeof(CaseInsensitive);

    internal static Levenshtrie<T> Create(IEnumerable<KeyValuePair<string, T>> source)
    {
        var root = new Entry
        {
            EntryValue = Rune.ReplacementChar,
            ResultIndex = NoIndex,
            FirstChildEntryIndex = NoIndex,
            TailDataIndex = NoIndex,
            NextSiblingEntryIndex = NoIndex,
            TailDataLength = 0
        };

        var trie = new Levenshtrie<T, TCaseSensitivity>([root], [], []);

        foreach (var (key, value) in source)
        {
            trie.Add(key, value);
        }

        return trie;
    }

    private bool SearchNode<TSearchState>(in Entry entry, TSearchState searchState, out TSearchState nextSearchState)
        where TSearchState : ILevenshtomatonExecutionState<TSearchState>
    {
        if (!searchState.MoveNext(entry.EntryValue, out nextSearchState))
        {
            return false;
        }

        if (entry.TailDataLength > 0)
        {
            var tailData = _tailData.AsSpan(entry.TailDataIndex, entry.TailDataLength);

            foreach (var tailDataRune in tailData.EnumerateRunes())
            {
                if (!nextSearchState.MoveNext(tailDataRune, out nextSearchState))
                {
                    return false;
                }
            }

        }

        return true;
    }

    public override bool TryGetValue(string key, [MaybeNullWhen(false)] out T value)
    {
        var entries = _entries;

        ref readonly var entry = ref _entries[0];
        var keySpan = key.AsSpan();

        while (keySpan.Length > 0)
        {
            Rune.DecodeFromUtf16(keySpan, out var nextRune, out var charsConsumed);
            keySpan = keySpan[charsConsumed..];

            var found = false;
            var nextChildEntryIndex = entry.FirstChildEntryIndex;
            while (nextChildEntryIndex != NoIndex)
            {
                entry = ref _entries[nextChildEntryIndex];

                if (default(TCaseSensitivity).Equals(entry.EntryValue, nextRune))
                {
                    found = true;

                    var entryTailDataLength = entry.TailDataLength;
                    if (entryTailDataLength > 0)
                    {
                        var tailData = _tailData.AsSpan(entry.TailDataIndex, entry.TailDataLength);
                        while (tailData.Length > 0 && keySpan.Length > 0)
                        {
                            Rune.DecodeFromUtf16(keySpan, out nextRune, out charsConsumed);
                            keySpan = keySpan[charsConsumed..];

                            Rune.DecodeFromUtf16(tailData, out var tailRune, out charsConsumed);
                            tailData = tailData[charsConsumed..];

                            if (!default(TCaseSensitivity).Equals(nextRune, tailRune))
                            {
                                found = false;
                                break;
                            }
                        }

                        if (tailData.Length > 0)
                        {
                            // We've consumed the key without consuming the tail data
                            found = false;
                        }
                    }

                    break;
                }

                nextChildEntryIndex = entry.NextSiblingEntryIndex;
            }

            if (!found)
            {
                goto NotFound;
            }
        }

        if (entry.ResultIndex >= 0)
        {
            value = _results[entry.ResultIndex];
            return true;
        }

    NotFound:

        value = default;
        return false;
    }

    public override LevenshtrieSearchResult<T>[] Search<TSearchState>(TSearchState searcher)
    {
        // This algorithm is recursive but that means that there's a risk of StackOverflow.
        // Thus we break recursion at a certain depth.
        const int MaxStackDepth = 20;

        var results = new HashSet<LevenshtrieSearchResult<T>>(LevenshtrieSearchResultComparer<T>.Instance);
        Queue<(int entryIndex, TSearchState searchState)>? processQueue = null;
        TraverseChildrenOf(0, searcher, MaxStackDepth);

        if (processQueue is not null)
        {
            // Non-recursive fallback
            while (processQueue.Count > 0)
            {
                var (entryIndex, searchState) = processQueue.Dequeue();

                TraverseChildrenOf(entryIndex, searchState, MaxStackDepth);
            }
        }

        return results.ToArray();

        void TraverseChildrenOf(int entryIndex, TSearchState searchState, int depthLeft)
        {
            ref readonly var entry = ref _entries[entryIndex];

            if (searchState.IsFinal && entry.ResultIndex >= 0)
            {
                results.Add(new LevenshtrieSearchResult<T>(searchState.Distance, _results[entry.ResultIndex]));
            }

            var nextChildEntryIndex = entry.FirstChildEntryIndex;
            while (nextChildEntryIndex >= 0)
            {
                ref readonly var childEntry = ref _entries[nextChildEntryIndex];

                if (SearchNode(in childEntry, searchState, out var nextSearchState))
                {
                    if (depthLeft > 0)
                    {
                        TraverseChildrenOf(nextChildEntryIndex, nextSearchState, depthLeft - 1);
                    }
                    else
                    {
                        processQueue ??= new();
                        processQueue.Enqueue((nextChildEntryIndex, nextSearchState));
                    }
                }

                nextChildEntryIndex = childEntry.NextSiblingEntryIndex;
            }
        }
    }

    public override IEnumerable<LevenshtrieSearchResult<T>> EnumerateSearch<TSearchState>(TSearchState searcher)
        => new SearchEnumerable<TSearchState>(this, searcher);

    private class SearchEnumerable<TSearchState> : IEnumerable<LevenshtrieSearchResult<T>>
        where TSearchState : ILevenshtomatonExecutionState<TSearchState>
    {
        private readonly Levenshtrie<T, TCaseSensitivity> _trie;
        private readonly TSearchState _initialState;

        public SearchEnumerable(Levenshtrie<T, TCaseSensitivity> trie, TSearchState initialState)
        {
            _trie = trie;
            _initialState = initialState;
        }

        public IEnumerator<LevenshtrieSearchResult<T>> GetEnumerator() => new SearchEnumerator<TSearchState>(_trie, _initialState);

        IEnumerator IEnumerable.GetEnumerator() => new SearchEnumerator<TSearchState>(_trie, _initialState);
    }

    private class SearchEnumerator<TSearchState> : IEnumerator<LevenshtrieSearchResult<T>>
        where TSearchState : ILevenshtomatonExecutionState<TSearchState>
    {
        private readonly Levenshtrie<T, TCaseSensitivity> _trie;
        private readonly Stack<(int entryIndex, TSearchState searchState)> _state = new();

        public SearchEnumerator(Levenshtrie<T, TCaseSensitivity> trie, TSearchState initialState)
        {
            _trie = trie;
            _state.Push((entryIndex: 0, searchState: initialState));
        }

        public LevenshtrieSearchResult<T> Current { get; private set; }

        object IEnumerator.Current => Current;

        public void Dispose() { }

        public bool MoveNext()
        {
            while (_state.TryPop(out var state))
            {
                var (entryIndex, searchState) = state;

                ref readonly var entry = ref _trie._entries[entryIndex];

                // Do not advance the automaton for the root
                if (entryIndex > 0)
                {
                    if (entry.NextSiblingEntryIndex >= 0)
                    {
                        _state.Push((entryIndex: entry.NextSiblingEntryIndex, searchState: searchState));
                    }

                    if (!_trie.SearchNode<TSearchState>(in entry, searchState, out searchState))
                    {
                        continue;
                    }
                }

                if (entry.FirstChildEntryIndex >= 0)
                {
                    _state.Push((entryIndex: entry.FirstChildEntryIndex, searchState: searchState));
                }

                if (searchState.IsFinal && entry.ResultIndex >= 0)
                {
                    Current = new LevenshtrieSearchResult<T>(searchState.Distance, _trie._results[entry.ResultIndex]);
                    return true;
                }
            }

            Current = default;
            return false;
        }

        public void Reset() => throw new NotSupportedException();
    }

    public override void Add(string key, T value) => Set(key, value, isUpdating: false);

    public override T this[string key]
    {
        get
        {
            if (TryGetValue(key, out var result))
            {
                return result;
            }

            throw new ArgumentOutOfRangeException(nameof(key));
        }
        set
        {
            Set(key, value, isUpdating: true);
        }
    }

    public void Set(string key, T value, bool isUpdating)
    {
        EnsureEntriesHasEmptySlots(2);

        var entries = _entries;
        ref var entry = ref _entries[0];
        var keySpan = key.AsSpan();

        while (keySpan.Length > 0)
        {
            Rune.DecodeFromUtf16(keySpan, out var nextRune, out var charsConsumed);
            keySpan = keySpan[charsConsumed..];

            var found = false;

            var nextChildEntryIndex = entry.FirstChildEntryIndex;
            while (nextChildEntryIndex != NoIndex)
            {
                ref var childEntry = ref _entries[nextChildEntryIndex];

                if (default(TCaseSensitivity).Equals(childEntry.EntryValue, nextRune))
                {
                    entry = ref childEntry;
                    found = true;

                    var isBranch = true;
                    var charsMatched = nextRune.Utf16SequenceLength;

                    var entryTailDataLength = childEntry.TailDataLength;
                    if (entryTailDataLength > 0)
                    {
                        var tailData = _tailData.AsSpan(childEntry.TailDataIndex, childEntry.TailDataLength);
                        while (tailData.Length > 0 && keySpan.Length > 0)
                        {
                            Rune.DecodeFromUtf16(keySpan, out nextRune, out charsConsumed);
                            keySpan = keySpan[charsConsumed..];

                            Rune.DecodeFromUtf16(tailData, out var tailRune, out charsConsumed);
                            tailData = tailData[charsConsumed..];

                            if (!default(TCaseSensitivity).Equals(nextRune, tailRune))
                            {
                                found = false;
                                break;
                            }

                            charsMatched += charsConsumed;
                        }

                        if (found && tailData.Length > 0)
                        {
                            // We've consumed the key without consuming the tail data
                            found = false;
                            isBranch = false;
                        }
                    }

                    if (!found)
                    {
                        if (!isBranch)
                        {
                            // The new node is along this head / tail data
                            Branch(ref childEntry, charsMatched, null, [], value);
                        }
                        else
                        {
                            // The new node branches off at some point along this head / tail data
                            Branch(ref childEntry, charsMatched, nextRune, keySpan, value);
                        }

                        return;
                    }

                    break;
                }

                nextChildEntryIndex = childEntry.NextSiblingEntryIndex;
            }

            if (!found)
            {
                Branch(
                    ref entry,
                    entry.EntryValue.Utf16SequenceLength + entry.TailDataLength,
                    nextRune,
                    keySpan,
                    value);
                return;
            }
        }

        if (entry.ResultIndex == NoIndex)
        {
            EnsureResultsHasEmptySlots(spacesRequired: 1);

            // Write result to results array
            var resultIndex = _resultsSize;
            _results[resultIndex] = value;
            _resultsSize++;

            entry.ResultIndex = resultIndex;
        }
        else if (isUpdating)
        {
            _results[entry.ResultIndex] = value;
        }
        else
        {
            throw new ArgumentException("May not use this data structure with duplicate keys.", nameof(key));
        }
    }

    public override void Remove(string key)
    {
        var entries = _entries;

        ref var entry = ref _entries[0];
        var keySpan = key.AsSpan();

        while (keySpan.Length > 0)
        {
            Rune.DecodeFromUtf16(keySpan, out var nextRune, out var charsConsumed);
            keySpan = keySpan[charsConsumed..];

            var found = false;
            var nextChildEntryIndex = entry.FirstChildEntryIndex;
            while (nextChildEntryIndex != NoIndex)
            {
                entry = ref _entries[nextChildEntryIndex];

                if (default(TCaseSensitivity).Equals(entry.EntryValue, nextRune))
                {
                    found = true;

                    var entryTailDataLength = entry.TailDataLength;
                    if (entryTailDataLength > 0)
                    {
                        var tailData = _tailData.AsSpan(entry.TailDataIndex, entry.TailDataLength);
                        while (tailData.Length > 0 && keySpan.Length > 0)
                        {
                            Rune.DecodeFromUtf16(keySpan, out nextRune, out charsConsumed);
                            keySpan = keySpan[charsConsumed..];

                            Rune.DecodeFromUtf16(tailData, out var tailRune, out charsConsumed);
                            tailData = tailData[charsConsumed..];

                            if (!default(TCaseSensitivity).Equals(nextRune, tailRune))
                            {
                                return;
                            }
                        }

                        if (tailData.Length > 0)
                        {
                            // We've consumed the key without consuming the tail data
                            return;
                        }
                    }

                    break;
                }

                nextChildEntryIndex = entry.NextSiblingEntryIndex;
            }

            if (!found)
            {
                return;
            }
        }

        if (entry.ResultIndex >= 0)
        {
            _results[entry.ResultIndex] = default!;
            entry.ResultIndex = -1;
        }
    }

    private void Branch(
        ref Entry parentEntry,
        int splitParentEntryChars,
        Rune? newBranchRune,
        ReadOnlySpan<char> newBranchTailData,
        T value)
    {
        // Write result to results array
        EnsureResultsHasEmptySlots(1);
        var resultIndex = _resultsSize;
        _results[_resultsSize++] = value;

        Debug.Assert(
            splitParentEntryChars >= parentEntry.EntryValue.Utf16SequenceLength 
            && splitParentEntryChars <= parentEntry.EntryValue.Utf16SequenceLength + parentEntry.TailDataLength);

        if (splitParentEntryChars < parentEntry.EntryValue.Utf16SequenceLength + parentEntry.TailDataLength)
        {
            // Split the node mid-way through the tail data
            var parentTailData = _tailData.AsSpan(parentEntry.TailDataIndex, parentEntry.TailDataLength);
            var parentTailDataKeep = splitParentEntryChars - parentEntry.EntryValue.Utf16SequenceLength;
            parentTailData = parentTailData.Slice(parentTailDataKeep);
            Rune.DecodeFromUtf16(parentTailData, out var tailDataHeadRune, out var tailDataHeadConsumed);

            // Tail data should be moved to its own node
            _entries[_entriesSize] = new Entry
            {
                EntryValue = tailDataHeadRune,
                FirstChildEntryIndex = parentEntry.FirstChildEntryIndex,
                NextSiblingEntryIndex = NoIndex,
                ResultIndex = parentEntry.ResultIndex,
                TailDataIndex = parentEntry.TailDataIndex + (parentTailDataKeep + tailDataHeadConsumed),
                TailDataLength = parentEntry.TailDataLength - (parentTailDataKeep + tailDataHeadConsumed)
            };

            parentEntry.FirstChildEntryIndex = _entriesSize;
            parentEntry.ResultIndex = NoIndex;

            if (parentTailDataKeep > 0)
            {
                parentEntry.TailDataLength = parentTailDataKeep;
            }
            else
            {
                parentEntry.TailDataIndex = NoIndex;
                parentEntry.TailDataLength = 0;
            }

            _entriesSize++;
        }
        else
        {
            Debug.Assert(newBranchRune is not null, "If we leave the parent alone, we should be adding a new node.");
            // No need to split the parent!
            // The new node can just be appended as a child.
        }

        if (newBranchRune is not null)
        {
            // We need to create a new node

            // Write tail data to tail data array
            var tailDataIndex = NoIndex;
            if (newBranchTailData.Length > 0)
            {
                EnsureTailDataHasEmptySlots(newBranchTailData.Length);

                tailDataIndex = _tailDataSize;
                newBranchTailData.CopyTo(_tailData.AsSpan(_tailDataSize..));
                _tailDataSize += newBranchTailData.Length;
            }

            _entries[_entriesSize] = new Entry
            {
                EntryValue = newBranchRune.Value,
                FirstChildEntryIndex = NoIndex,
                NextSiblingEntryIndex = NoIndex,
                ResultIndex = resultIndex,
                TailDataIndex = tailDataIndex,
                TailDataLength = newBranchTailData.Length
            };

            if (parentEntry.FirstChildEntryIndex is var childEntryIndex && childEntryIndex is not NoIndex)
            {
                while (_entries[childEntryIndex].NextSiblingEntryIndex is var nextSiblingEntryIndex && nextSiblingEntryIndex is not NoIndex)
                {
                    childEntryIndex = nextSiblingEntryIndex;
                }

                _entries[childEntryIndex].NextSiblingEntryIndex = _entriesSize;
            }
            else
            {
                parentEntry.FirstChildEntryIndex = _entriesSize;
            }

            _entriesSize++;
        }
        else
        {
            Debug.Assert(parentEntry.ResultIndex == NoIndex);
            parentEntry.ResultIndex = resultIndex;
        }
    }

    private void EnsureEntriesHasEmptySlots(int spacesRequired)
    {
        while (_entries.Length - _entriesSize < spacesRequired)
        {
            Array.Resize(ref _entries, NextArraySize(_entries.Length));
        }
    }

    private void EnsureResultsHasEmptySlots(int spacesRequired)
    {
        while (_results.Length - _resultsSize < spacesRequired)
        {
            Array.Resize(ref _results, NextArraySize(_results.Length));
        }
    }

    private void EnsureTailDataHasEmptySlots(int spacesRequired)
    {
        while (_tailData.Length - _tailDataSize < spacesRequired)
        {
            Array.Resize(ref _tailData, NextArraySize(_tailData.Length));
        }
    }

    private int NextArraySize(int currentSize) =>
        currentSize switch
        {
            < 16 => 16,
            _ => currentSize * 2
        };

    [DebuggerDisplay("{EntryValue}")]
    private struct Entry
    {
        public Rune EntryValue;
        public int TailDataIndex;
        public int TailDataLength;
        public int FirstChildEntryIndex;
        public int NextSiblingEntryIndex;
        public int ResultIndex;
    }
}
