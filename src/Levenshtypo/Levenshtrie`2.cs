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

    private struct CreationQueueEntry
    {
        public int EntryIndex;
        public Rune Value;
        public int GroupIndex;
        public int GroupLength;
        public int NextSiblingIndex;
    }

    internal static Levenshtrie<T> Create(IEnumerable<KeyValuePair<string, T>> source)
    {
        var flatSource = source.Select(s => new InputKvp(s)).ToArray();

        const int ArbitraryMultiplicationFactor = 5;

        var inputKvps = new InputKvp[flatSource.Length];
        for (int i = 0; i < flatSource.Length; i++)
        {
            inputKvps[i].Key = flatSource[i].Key;
            inputKvps[i].Value = flatSource[i].Value;
        }

        var entries = new List<Entry>(flatSource.Length * ArbitraryMultiplicationFactor);
        var results = new List<T>(flatSource.Length);
        var tailData = new List<char>(flatSource.Length * ArbitraryMultiplicationFactor);
        var processQueue = new Queue<CreationQueueEntry>();

        Array.Sort(flatSource, (a, b) => default(TCaseSensitivity).KeyComparer.Compare(a.Key, b.Key));

        if (flatSource.Length > 0)
        {
            entries.Add(new Entry());

            processQueue.Enqueue(new CreationQueueEntry
            {
                EntryIndex = 0,
                Value = Rune.ReplacementChar,
                GroupIndex = 0,
                GroupLength = flatSource.Length,
                NextSiblingIndex = NoIndex
            });

            while (processQueue.Count > 0)
            {
                AppendChildren(processQueue.Dequeue());
            }
        }
        else
        {
            entries.Add(new Entry
            {
                EntryValue = Rune.ReplacementChar,
                ResultIndex = NoIndex,
                FirstChildEntryIndex = NoIndex,
                TailDataIndex = NoIndex,
                NextSiblingEntryIndex = NoIndex,
                TailDataLength = 0
            });
        }

        return new Levenshtrie<T, TCaseSensitivity>(entries.ToArray(), results.ToArray(), tailData.ToArray());

        void AppendChildren(CreationQueueEntry processEntry)
        {
            var group = flatSource.AsSpan(processEntry.GroupIndex, processEntry.GroupLength);

            int resultIndex = NoIndex;
            var tailDataIndex = tailData.Count;
            var firstItemNextReadIndex = group[0].NextReadIndex;
            var lastCharsRead = 0;
            int tailDataLength = 0;
            var discriminatorOffset = 0;
            var childGroups = new List<(Rune rune, int rangeStartIndex, int rangeEndIndexExcl)>();
            do
            {
                discriminatorOffset++;

                childGroups.Clear();

                Rune currentDiscriminator = default;
                var currentRangeStartIndex = 0;
                var currentRangeEndIndexExcl = 0;

                tailDataLength += lastCharsRead;
                var isFirst = true;

                for (var gIndex = 0; gIndex < group.Length; gIndex++)
                {
                    ref var item = ref group[gIndex];
                    if (item.Key.Length == item.NextReadIndex)
                    {
                        if (resultIndex != NoIndex)
                        {
                            throw new ArgumentException("May not use this data structure with duplicate keys.", nameof(source));
                        }

                        resultIndex = results.Count;
                        results.Add(item.Value);
                    }
                    else
                    {
                        Rune.DecodeFromUtf16(item.Key.AsSpan(item.NextReadIndex), out var discriminator, out var charsRead);
                        item.NextReadIndex += charsRead;

                        if (isFirst)
                        {
                            lastCharsRead = charsRead;
                        }

                        if (default(TCaseSensitivity).Equals(discriminator, currentDiscriminator))
                        {
                            currentRangeEndIndexExcl = gIndex + 1;
                        }
                        else
                        {
                            if (currentRangeStartIndex != 0 || currentRangeEndIndexExcl != 0)
                            {
                                childGroups.Add((currentDiscriminator, currentRangeStartIndex, currentRangeEndIndexExcl));
                            }
                            currentRangeStartIndex = gIndex;
                            currentRangeEndIndexExcl = gIndex + 1;
                            currentDiscriminator = discriminator;
                        }
                    }
                }

                if (currentRangeStartIndex != 0 || currentRangeEndIndexExcl != 0)
                {
                    childGroups.Add((currentDiscriminator, currentRangeStartIndex, currentRangeEndIndexExcl));
                }

            } while (resultIndex == NoIndex && childGroups.Count == 1 && firstItemNextReadIndex > 0);

            if (tailDataLength > 0)
            {
                var entryTailData = group[0].Key.AsSpan(firstItemNextReadIndex, tailDataLength);

#if NET8_0_OR_GREATER
                tailData.AddRange(entryTailData);
#else
                tailData.AddRange(entryTailData.ToArray());
#endif
            }

            int childEntriesStartIndex = entries.Count;

            for (int i = 0; i < childGroups.Count; i++)
            {
                var (entryDiscriminator, rangeStartIndex, rangeEndIndexExcl) = childGroups[i];

                entries.Add(new Entry());
                processQueue.Enqueue(new CreationQueueEntry
                {
                    EntryIndex = entries.Count - 1,
                    Value = entryDiscriminator,
                    GroupIndex = processEntry.GroupIndex + rangeStartIndex,
                    GroupLength = rangeEndIndexExcl - rangeStartIndex,
                    NextSiblingIndex = i == childGroups.Count - 1 ? NoIndex : entries.Count
                });
            }

            entries[processEntry.EntryIndex] = new Entry
            {
                EntryValue = processEntry.Value,
                ResultIndex = resultIndex,
                FirstChildEntryIndex = childGroups.Count == 0 ? NoIndex : childEntriesStartIndex,
                NextSiblingEntryIndex = processEntry.NextSiblingIndex,
                TailDataIndex = tailDataLength == 0 ? NoIndex : tailDataIndex,
                TailDataLength = tailDataLength
            };
        }
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
        ref readonly var entry = ref entries[0];

        var executionState = DirectState.Start(key);

        while (!executionState.IsFinal)
        {
            var found = false;

            var nextChildEntryIndex = entry.FirstChildEntryIndex;

            while (nextChildEntryIndex >= 0)
            {
                entry = ref entries[nextChildEntryIndex];

                if (SearchNode(in entry, executionState, out var nextExecutionState))
                {
                    executionState = nextExecutionState;
                    found = true;
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
        ref var entry = ref entries[0];

        var executionState = DirectState.Start(key);

        while (!executionState.IsFinal)
        {
            var found = false;

            var nextChildEntryIndex = entry.FirstChildEntryIndex;

            while (nextChildEntryIndex >= 0)
            {
                ref var childEntry = ref entries[nextChildEntryIndex];

                if (SearchNode(in childEntry, executionState, out var nextExecutionState))
                {
                    entry = ref childEntry;
                    executionState = nextExecutionState;
                    found = true;
                    break;
                }


                var sharedChars = nextExecutionState.NumCharsMatched - executionState.NumCharsMatched;
                if (sharedChars > 0)
                {
                    if (nextExecutionState.IsFinal)
                    {
                        // The new node is along this head / tail data
                        Debug.Assert(nextExecutionState.NextRune == Rune.ReplacementChar);
                        Branch(ref childEntry, sharedChars, null, [], value);
                    }
                    else
                    {
                        // The new node branches off at some point along this head / tail data
                        Branch(ref childEntry, sharedChars, nextExecutionState.NextRune, key.AsSpan(nextExecutionState.UnmatchedOffset), value);
                    }
                    return;
                }

                nextChildEntryIndex = childEntry.NextSiblingEntryIndex;
            }

            if (!found)
            {
                // Add a new child entry as a direct child of this one.
                Branch(
                    ref entry,
                    entry.EntryValue.Utf16SequenceLength + entry.TailDataLength,
                    executionState.NextRune,
                    key.AsSpan(executionState.UnmatchedOffset),
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
        ref var entry = ref entries[0];

        var executionState = DirectState.Start(key);

        while (!executionState.IsFinal)
        {
            var found = false;

            var nextChildEntryIndex = entry.FirstChildEntryIndex;

            while (nextChildEntryIndex >= 0)
            {
                entry = ref entries[nextChildEntryIndex];

                if (SearchNode(in entry, executionState, out var nextExecutionState))
                {
                    executionState = nextExecutionState;
                    found = true;
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

    private readonly struct DirectState : ILevenshtomatonExecutionState<DirectState>
    {
        private readonly string _s;
        private readonly int _sIndex;
        private readonly Rune _nextRune;

        private DirectState(string s, int sIndex, Rune nextRune)
        {
            _s = s;
            _sIndex = sIndex;
            _nextRune = nextRune;
        }

        public static DirectState Start(string s)
        {
            Rune.DecodeFromUtf16(s, out var sNext, out _);
            return new DirectState(s, 0, sNext);
        }

        public bool MoveNext(Rune c, out DirectState next)
        {
            var s = _s;
            var sIndex = _sIndex;

            if (sIndex < s.Length && default(TCaseSensitivity).Equals(_nextRune, c))
            {
                var prevConsumed = _nextRune.IsBmp ? 1 : 2;
                var nextSIndex = _sIndex + prevConsumed;

                Rune.DecodeFromUtf16(s.AsSpan(nextSIndex), out var nextRune, out _);
                next = new DirectState(s, nextSIndex, nextRune);
                return true;
            }

            next = this;
            return false;
        }

        public int NumCharsMatched => _sIndex;

        public int UnmatchedOffset => _sIndex + (_nextRune.IsBmp ? 1 : 2);

        public Rune NextRune => _nextRune;

        public bool IsFinal => _sIndex == _s.Length;

        public int Distance => 0;
    }

    private struct InputKvp
    {
        public InputKvp(KeyValuePair<string, T> input)
        {
            Key = input.Key;
            Value = input.Value;
        }

        public string Key;
        public int NextReadIndex;
        public T Value;
    }

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
