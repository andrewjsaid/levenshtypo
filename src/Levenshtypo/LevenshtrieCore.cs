using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace Levenshtypo;

internal abstract class LevenshtrieCore<T>
{
    private const int NoIndex = -1;

    private Entry[] _entries;
    private int _entriesSize;

    private Result[] _results = [];
    private int _resultsSize;
    private readonly bool _allowMulti;
    private int _freeResultHeadIndex = NoIndex;
    private int _freeResultCount;

    private char[] _tailData = [];
    private int _tailDataSize;

    private protected LevenshtrieCore(Entry root, bool allowMulti)
    {
        _entries = [root];
        _entriesSize = 1;
        _allowMulti = allowMulti;
    }

    internal static LevenshtrieCore<T> Create(
        IEnumerable<KeyValuePair<string, T>> source,
        bool ignoreCase,
        bool allowMulti)
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

        LevenshtrieCore<T> trie = ignoreCase
            ? new CaseInsensitiveLevenshtrieCore<T>(root, allowMulti)
            : new CaseSensitiveLevenshtrieCore<T>(root, allowMulti);

        foreach (var (key, value) in source)
        {
            trie.Set(key, value, overwrite: false);
        }

        return trie;
    }

    public abstract bool IgnoreCase { get; }

    private protected abstract bool AreEqual(Rune a, Rune b);

    private protected abstract bool AreEqual(char a, char b);

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

    private bool SearchNodeByPrefix<TSearchState>(in Entry entry, TSearchState searchState, out TSearchState nextSearchState)
        where TSearchState : ILevenshtomatonExecutionState<TSearchState>
    {
        if (!searchState.MoveNext(entry.EntryValue, out nextSearchState))
        {
            return false;
        }

        if (nextSearchState.IsFinal)
        {
            return true;
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

                if (nextSearchState.IsFinal)
                {
                    return true;
                }
            }

        }

        return true;
    }

    public Cursor GetValues(string key)
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

                if (AreEqual(entry.EntryValue, nextRune))
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

                            if (!AreEqual(nextRune, tailRune))
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

        if (entry.ResultIndex is not NoIndex)
        {
            return new Cursor(_results, entry.ResultIndex);
        }

    NotFound:

        return new Cursor(_results, NoIndex);
    }

    public LevenshtrieSearchResult<T>[] Search<TSearchState>(TSearchState searcher)
        where TSearchState : ILevenshtomatonExecutionState<TSearchState>
    {
        // This algorithm is recursive but that means that there's a risk of StackOverflow.
        // Thus we break recursion at a certain depth.
        const int MaxStackDepth = 20;

        var results = new List<LevenshtrieSearchResult<T>>();
        Queue<(int entryIndex, TSearchState searchState)>? processQueue = null;
        Process(0, searcher, MaxStackDepth);

        if (processQueue is not null)
        {
            while (processQueue.Count > 0)
            {
                var (entryIndex, searchState) = processQueue.Dequeue();

                Process(entryIndex, searchState, MaxStackDepth);
            }
        }

        return results.ToArray();

        void Process(int entryIndex, TSearchState searchState, int depthLeft)
        {
            if (depthLeft == 0)
            {
                processQueue ??= new();
                processQueue.Enqueue((entryIndex, searchState));
                return;
            }

            ref readonly var entry = ref _entries[entryIndex];

            if (searchState.IsFinal && entry.ResultIndex is not NoIndex)
            {
                var cursor = new Cursor(_results, entry.ResultIndex);
                while (cursor.MoveNext(out var result))
                {
                    results.Add(new LevenshtrieSearchResult<T>(searchState.Distance, result));
                }
            }

            var nextChildEntryIndex = entry.FirstChildEntryIndex;
            while (nextChildEntryIndex is not NoIndex)
            {
                ref readonly var childEntry = ref _entries[nextChildEntryIndex];

                if (SearchNode(in childEntry, searchState, out var nextSearchState))
                {
                    Process(nextChildEntryIndex, nextSearchState, depthLeft - 1);
                }

                nextChildEntryIndex = childEntry.NextSiblingEntryIndex;
            }
        }
    }

    public T[] SearchByPrefix<TSearchState>(TSearchState searcher)
        where TSearchState : ILevenshtomatonExecutionState<TSearchState>
    {
        // This algorithm is recursive but that means that there's a risk of StackOverflow.
        // Thus we break recursion at a certain depth.
        const int MaxStackDepth = 20;

        var results = new List<T>();
        Queue<(int entryIndex, TSearchState searchState)>? processQueue = null;
        Stack<int>? resultStack = null;
        Process(0, searcher, MaxStackDepth);

        if (processQueue is not null)
        {
            while (processQueue.Count > 0)
            {
                var (entryIndex, searchState) = processQueue.Dequeue();

                Process(entryIndex, searchState, MaxStackDepth);
            }
        }

        return results.ToArray();

        void Process(int entryIndex, TSearchState searchState, int depthLeft)
        {
            if (depthLeft == 0)
            {
                processQueue ??= new();
                processQueue.Enqueue((entryIndex, searchState));
                return;
            }

            ref readonly var entry = ref _entries[entryIndex];

            if (searchState.IsFinal)
            {
                AddWithDescendants(in entry);
            }
            else
            {
                var nextChildEntryIndex = entry.FirstChildEntryIndex;
                while (nextChildEntryIndex is not NoIndex)
                {
                    ref readonly var childEntry = ref _entries[nextChildEntryIndex];

                    if (SearchNodeByPrefix(in childEntry, searchState, out var nextSearchState))
                    {
                        if (nextSearchState.IsFinal)
                        {
                            AddWithDescendants(in childEntry);
                        }
                        else
                        {
                            Process(nextChildEntryIndex, nextSearchState, depthLeft - 1);
                        }
                    }

                    nextChildEntryIndex = childEntry.NextSiblingEntryIndex;
                }
            }
        }

        void AddWithDescendants(ref readonly Entry entry)
        {
            if (entry.ResultIndex is not NoIndex)
            {
                var cursor = new Cursor(_results, entry.ResultIndex);
                while (cursor.MoveNext(out var result))
                {
                    results.Add(result);
                }
            }

            if (entry.FirstChildEntryIndex is not NoIndex)
            {
                resultStack ??= new();
                resultStack.Push(entry.FirstChildEntryIndex);

                while (resultStack.TryPop(out var nextIndex))
                {
                    ref readonly var nextEntry = ref _entries[nextIndex];

                    if (nextEntry.NextSiblingEntryIndex is not NoIndex)
                    {
                        resultStack.Push(nextEntry.NextSiblingEntryIndex);
                    }

                    if (nextEntry.FirstChildEntryIndex is not NoIndex)
                    {
                        resultStack.Push(nextEntry.FirstChildEntryIndex);
                    }

                    if (nextEntry.ResultIndex is not NoIndex)
                    {
                        var cursor = new Cursor(_results, nextEntry.ResultIndex);
                        while (cursor.MoveNext(out var result))
                        {
                            results.Add(result);
                        }
                    }
                }
            }
        }

        static int FindFirstResultIndex(Entry[] entries, int entryIndex)
        {
            ref readonly var entry = ref entries[entryIndex];

            var resultIndex = entry.ResultIndex;

            if (resultIndex is NoIndex && entry.FirstChildEntryIndex is not NoIndex)
            {
                resultIndex = FindFirstResultIndex(entries, entry.FirstChildEntryIndex);
            }

            if (resultIndex is NoIndex && entry.NextSiblingEntryIndex is not NoIndex)
            {
                resultIndex = FindFirstResultIndex(entries, entry.NextSiblingEntryIndex);
            }

            return resultIndex;
        }

        static int FindLastResultIndex(Entry[] entries, int entryIndex)
        {
            ref readonly var entry = ref entries[entryIndex];

            var resultIndex = NoIndex;

            if (resultIndex is NoIndex && entry.NextSiblingEntryIndex is not NoIndex)
            {
                resultIndex = FindLastResultIndex(entries, entry.NextSiblingEntryIndex);
            }

            if (resultIndex is NoIndex && entry.FirstChildEntryIndex is not NoIndex)
            {
                resultIndex = FindLastResultIndex(entries, entry.FirstChildEntryIndex);
            }

            if (resultIndex is NoIndex && entry.ResultIndex is not NoIndex)
            {
                resultIndex = entry.ResultIndex;
            }

            return resultIndex;
        }
    }

    public IEnumerable<LevenshtrieSearchResult<T>> EnumerateSearch<TSearchState>(TSearchState searcher)
        where TSearchState : ILevenshtomatonExecutionState<TSearchState>
        => new SearchEnumerable<TSearchState>(this, searcher);

    private class SearchEnumerable<TSearchState> : IEnumerable<LevenshtrieSearchResult<T>>
        where TSearchState : ILevenshtomatonExecutionState<TSearchState>
    {
        private readonly LevenshtrieCore<T> _trie;
        private readonly TSearchState _initialState;

        public SearchEnumerable(LevenshtrieCore<T> trie, TSearchState initialState)
        {
            _trie = trie;
            _initialState = initialState;
        }

        public IEnumerator<LevenshtrieSearchResult<T>> GetEnumerator() => new SearchEnumerator<TSearchState>(_trie, _initialState);

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private class SearchEnumerator<TSearchState> : IEnumerator<LevenshtrieSearchResult<T>>
        where TSearchState : ILevenshtomatonExecutionState<TSearchState>
    {
        private readonly LevenshtrieCore<T> _trie;
        private readonly Stack<(int entryIndex, TSearchState searchState)> _state = new();
        private int _cursorDistance;
        private Cursor _cursor;

        public SearchEnumerator(LevenshtrieCore<T> trie, TSearchState initialState)
        {
            _trie = trie;
            _state.Push((entryIndex: 0, searchState: initialState));
            _cursor = new Cursor(trie._results, NoIndex);
        }

        public LevenshtrieSearchResult<T> Current { get; private set; }

        object System.Collections.IEnumerator.Current => Current;

        public void Dispose() { }

        public bool MoveNext()
        {
        restart:
            if (_cursor.MoveNext(out var result))
            {
                Current = new LevenshtrieSearchResult<T>(_cursorDistance, result);
                return true;
            }

            while (_state.TryPop(out var state))
            {
                var (entryIndex, searchState) = state;

                ref readonly var entry = ref _trie._entries[entryIndex];

                // Do not advance the automaton for the root
                if (entryIndex > 0)
                {
                    if (entry.NextSiblingEntryIndex is not NoIndex)
                    {
                        _state.Push((entryIndex: entry.NextSiblingEntryIndex, searchState: searchState));
                    }

                    if (!_trie.SearchNode<TSearchState>(in entry, searchState, out searchState))
                    {
                        continue;
                    }
                }

                if (entry.FirstChildEntryIndex is not NoIndex)
                {
                    _state.Push((entryIndex: entry.FirstChildEntryIndex, searchState: searchState));
                }

                if (searchState.IsFinal && entry.ResultIndex is not NoIndex)
                {
                    _cursorDistance = searchState.Distance;
                    _cursor.ResetTo(entry.ResultIndex);
                    goto restart;
                }
            }

            Current = default;
            return false;
        }

        public void Reset() => throw new NotSupportedException();
    }

    public IEnumerable<T> EnumerateSearchByPrefix<TSearchState>(TSearchState searcher)
        where TSearchState : ILevenshtomatonExecutionState<TSearchState>
        => new SearchByPrefixEnumerable<TSearchState>(this, searcher);

    private class SearchByPrefixEnumerable<TSearchState> : IEnumerable<T>
        where TSearchState : ILevenshtomatonExecutionState<TSearchState>
    {
        private readonly LevenshtrieCore<T> _trie;
        private readonly TSearchState _initialState;

        public SearchByPrefixEnumerable(LevenshtrieCore<T> trie, TSearchState initialState)
        {
            _trie = trie;
            _initialState = initialState;
        }

        public IEnumerator<T> GetEnumerator() => new SearchByPrefixEnumerator<TSearchState>(_trie, _initialState);

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private class SearchByPrefixEnumerator<TSearchState> : IEnumerator<T>
        where TSearchState : ILevenshtomatonExecutionState<TSearchState>
    {
        private readonly LevenshtrieCore<T> _trie;
        private readonly Stack<(int entryIndex, TSearchState searchState)> _state = new();
        private Stack<int>? _resultState = null;
        private Cursor _cursor;

        public SearchByPrefixEnumerator(LevenshtrieCore<T> trie, TSearchState initialState)
        {
            _trie = trie;
            _state.Push((entryIndex: 0, searchState: initialState));
            _cursor = new Cursor(trie._results, NoIndex);
        }

        public T Current { get; private set; } = default!;

        object System.Collections.IEnumerator.Current => Current!;

        public void Dispose() { }

        public bool MoveNext()
        {

        restart:
            if (_cursor.MoveNext(out var result))
            {
                Current = result;
                return true;
            }

            if (_resultState is not null)
            {
                while (_resultState.TryPop(out var resultEntryIndex))
                {
                    ref readonly var nextEntry = ref _trie._entries[resultEntryIndex];

                    if (nextEntry.NextSiblingEntryIndex is not NoIndex)
                    {
                        _resultState.Push(nextEntry.NextSiblingEntryIndex);
                    }

                    if (nextEntry.FirstChildEntryIndex is not NoIndex)
                    {
                        _resultState.Push(nextEntry.FirstChildEntryIndex);
                    }

                    if (nextEntry.ResultIndex is not NoIndex)
                    {
                        _cursor.ResetTo(nextEntry.ResultIndex);
                        goto restart;
                    }
                }
            }

            while (_state.TryPop(out var state))
            {
                var (entryIndex, searchState) = state;

                ref readonly var entry = ref _trie._entries[entryIndex];

                // Do not advance the automaton for the root
                if (entryIndex > 0)
                {
                    if (entry.NextSiblingEntryIndex is not NoIndex)
                    {
                        _state.Push((entryIndex: entry.NextSiblingEntryIndex, searchState: searchState));
                    }

                    if (!_trie.SearchNodeByPrefix<TSearchState>(in entry, searchState, out searchState))
                    {
                        continue;
                    }
                }

                if (searchState.IsFinal)
                {
                    _resultState ??= new();
                    if (entry.FirstChildEntryIndex is not NoIndex)
                    {
                        _resultState.Push(entry.FirstChildEntryIndex);
                    }

                    if (entry.ResultIndex is not NoIndex)
                    {
                        _cursor.ResetTo(entry.ResultIndex);
                        goto restart;
                    }

                    goto restart;
                }
                else
                {
                    if (entry.FirstChildEntryIndex is not NoIndex)
                    {
                        _state.Push((entryIndex: entry.FirstChildEntryIndex, searchState: searchState));
                    }
                }
            }

            Current = default!;
            return false;
        }

        public void Reset() => throw new NotSupportedException();
    }

    public void Set(string key, T value, bool overwrite)
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

                if (AreEqual(childEntry.EntryValue, nextRune))
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

                            if (!AreEqual(nextRune, tailRune))
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

        var existingResultIndex = entry.ResultIndex;
        if (existingResultIndex is not NoIndex && overwrite)
        {
            _results[existingResultIndex] = new Result
            {
                Value = value,
                NextResultIndex = NoIndex
            };
        }
        else if (existingResultIndex is NoIndex || _allowMulti)
        {
            // Write result to results array
            var resultIndex = TakeResultSlot();

            if (existingResultIndex is not NoIndex && _allowMulti)
            {
                ref Result finalResultEntry = ref Unsafe.NullRef<Result>();

                var nextExistingResultIndex = existingResultIndex;
                while (nextExistingResultIndex is not NoIndex)
                {
                    finalResultEntry = ref _results[nextExistingResultIndex];
                    nextExistingResultIndex = finalResultEntry.NextResultIndex;
                }

                finalResultEntry.NextResultIndex = resultIndex;
            }
            else
            {
                entry.ResultIndex = resultIndex;
            }

            _results[resultIndex] = new Result
            {
                Value = value,
                NextResultIndex = NoIndex
            };

            _resultsSize++;
        }
        else
        {
            throw new ArgumentException("May not use this data structure with duplicate keys.", nameof(key));
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
        var resultIndex = TakeResultSlot();
        _results[resultIndex] = new Result
        {
            Value = value,
            NextResultIndex = NoIndex
        };

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

    public bool Remove(string key, bool all, T? matchingValue, IEqualityComparer<T> comparer)
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

                if (AreEqual(entry.EntryValue, nextRune))
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

                            if (!AreEqual(nextRune, tailRune))
                            {
                                return false;
                            }
                        }

                        if (tailData.Length > 0)
                        {
                            // We've consumed the key without consuming the tail data
                            return false;
                        }
                    }

                    break;
                }

                nextChildEntryIndex = entry.NextSiblingEntryIndex;
            }

            if (!found)
            {
                return false;
            }
        }

        if (entry.ResultIndex is NoIndex)
        {
            return false;
        }

        // Here we need to find the right entries
        // to be deleted and repair the linked list.

        bool hasRemoved = false;

        ref Result currResult = ref Unsafe.NullRef<Result>();
        ref Result prevResult = ref Unsafe.NullRef<Result>();
        var nextResultIndex = entry.ResultIndex;

        while (nextResultIndex is not NoIndex)
        {
            currResult = ref _results[nextResultIndex];
            var currResultIndex = nextResultIndex;
            nextResultIndex = currResult.NextResultIndex;

            if (all || comparer.Equals(currResult.Value, matchingValue))
            {
                hasRemoved = true;
                AppendToFreeList(currResultIndex, ref currResult);
                currResult.Value = default!;
                if (Unsafe.IsNullRef(ref prevResult))
                {
                    entry.ResultIndex = nextResultIndex;
                }
                else
                {
                    prevResult.NextResultIndex = nextResultIndex;
                }
            }
            else
            {
                // We only move the prev result if it's not deleted
                // so that we can delete multiple consecutive entries
                prevResult = ref currResult;
            }
        }

        return hasRemoved;
    }

    private void AppendToFreeList(int resultIndex, ref Result result)
    {
        var prevFreeHeadIndex = _freeResultHeadIndex;
        _freeResultHeadIndex = resultIndex;
        result.NextResultIndex = prevFreeHeadIndex;
        _freeResultCount++;
    }

    private int TakeResultSlot()
    {
        var freeIndex = _freeResultHeadIndex;
        if (freeIndex is NoIndex)
        {
            EnsureResultsHasEmptySlots(spacesRequired: 1);
            return _resultsSize++;
        }
        else
        {
            _freeResultCount--;
            _freeResultHeadIndex = _results[freeIndex].NextResultIndex;
            return freeIndex;
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

    private static int NextArraySize(int currentSize) =>
        currentSize switch
        {
            < 16 => 16,
            _ => currentSize * 2
        };


    [DebuggerDisplay("{EntryValue}")]
    internal struct Entry
    {
        public Rune EntryValue;
        public int TailDataIndex;
        public int TailDataLength;
        public int FirstChildEntryIndex;
        public int NextSiblingEntryIndex;
        public int ResultIndex;
    }

    [DebuggerDisplay("{Value}")]
    internal struct Result
    {
        public T Value;
        public int NextResultIndex;
    }

    internal struct Cursor(Result[] results, int index)
    {
        private Result[] _results = results;
        private int _index = index;

        public void ResetTo(int index) => _index = index;

        public bool MoveNext([MaybeNullWhen(false)] out T value)
        {
            if (_index is NoIndex)
            {
                value = default;
                return false;
            }

            ref var result = ref _results[_index];
            value = result.Value;
            _index = result.NextResultIndex;
            return true;
        }
    }
}

internal sealed class CaseSensitiveLevenshtrieCore<T> : LevenshtrieCore<T>
{
    public CaseSensitiveLevenshtrieCore(Entry root, bool allowMulti)
        : base(root, allowMulti)
    {

    }

    public override bool IgnoreCase => false;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private protected override bool AreEqual(Rune a, Rune b)
        => default(CaseSensitive).Equals(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private protected override bool AreEqual(char a, char b)
        => default(CaseSensitive).Equals(a, b);
}

internal sealed class CaseInsensitiveLevenshtrieCore<T> : LevenshtrieCore<T>
{
    public override bool IgnoreCase => true;

    public CaseInsensitiveLevenshtrieCore(Entry root, bool allowMulti)
        : base(root, allowMulti)
    {

    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private protected override bool AreEqual(Rune a, Rune b)
        => default(CaseInsensitive).Equals(a, b);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private protected override bool AreEqual(char a, char b)
        => default(CaseInsensitive).Equals(a, b);
}