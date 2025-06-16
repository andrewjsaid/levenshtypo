using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace Levenshtypo;

/// <summary>
/// Represents the core logic for a high-performance trie that supports approximate string matching,
/// case sensitivity, and flexible result storage.
///
/// <para>
/// This abstract base class handles the core character traversal and storage mechanics of a trie
/// using <see cref="ReadOnlySpan{char}"/> as input keys. It maps normalized strings to an internal
/// <c>int resultIndex</c>, which acts as an opaque reference to the associated data.
/// </para>
///
/// <para>
/// Derived types are responsible for interpreting and managing the actual result storage using this
/// index — whether that means storing a single result, a collection, or using specialized memory layouts.
/// </para>
/// </summary>
/// <typeparam name="T">
/// The type of result values stored in the trie.
/// </typeparam>
/// <typeparam name="TCaseSensitivity">
/// A struct implementing <see cref="ICaseSensitivity{T}"/> that defines case sensitivity rules
/// used during character comparison.
/// </typeparam>
/// <typeparam name="TCursor">
/// A cursor type implementing <see cref="ILevenshtrieCursor{T}"/> that allows traversal over
/// stored results in the trie. Enables efficient enumeration and projection of results.
/// </typeparam>

internal abstract class LevenshtrieCore<T, TCaseSensitivity, TCursor>
    where TCaseSensitivity : struct, ICaseSensitivity<TCaseSensitivity>
    where TCursor : struct, ILevenshtrieCursor<T>
{
    protected const int NoIndex = -1;

    private LevenshtrieCoreEntry[] _entries;
    private int _entriesSize;

    private char[] _tailData = [];
    private int _tailDataSize;

    private protected LevenshtrieCore(LevenshtrieCoreEntry root)
    {
        _entries = [root];
        _entriesSize = 1;
    }

    protected abstract TCursor CreateCursor(int resultIndex);

    private bool SearchNode<TSearchState>(in LevenshtrieCoreEntry entry, TSearchState searchState, out TSearchState nextSearchState)
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

    protected ref LevenshtrieCoreEntry GetEntryRef(ReadOnlySpan<char> key)
    {
        var entries = _entries;

        ref var entry = ref _entries[0];

        while (key.Length > 0)
        {
            Rune.DecodeFromUtf16(key, out var nextRune, out var charsConsumed);
            key = key[charsConsumed..];

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
                        while (tailData.Length > 0 && key.Length > 0)
                        {
                            Rune.DecodeFromUtf16(key, out nextRune, out charsConsumed);
                            key = key[charsConsumed..];

                            Rune.DecodeFromUtf16(tailData, out var tailRune, out charsConsumed);
                            tailData = tailData[charsConsumed..];

                            if (!default(TCaseSensitivity).Equals(nextRune, tailRune))
                            {
                                return ref Unsafe.NullRef<LevenshtrieCoreEntry>();
                            }
                        }

                        if (tailData.Length > 0)
                        {
                            // We've consumed the key without consuming the tail data
                            return ref Unsafe.NullRef<LevenshtrieCoreEntry>();
                        }
                    }

                    break;
                }

                nextChildEntryIndex = entry.NextSiblingEntryIndex;
            }

            if (!found)
            {
                return ref Unsafe.NullRef<LevenshtrieCoreEntry>();
            }
        }

        if (entry.ResultIndex is NoIndex)
        {
            return ref Unsafe.NullRef<LevenshtrieCoreEntry>();
        }

        return ref entry;
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
                var cursor = CreateCursor(entry.ResultIndex);
                while (cursor.MoveNext(out var result))
                {
                    var searchKind = searchState.TryGetPrefixSearchMetadata(out var metadata) ? LevenshtrieSearchKind.Prefix : LevenshtrieSearchKind.Full;
                    results.Add(new LevenshtrieSearchResult<T>(result, searchState.Distance, searchKind, metadata));
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

    public IEnumerable<LevenshtrieSearchResult<T>> EnumerateSearch<TSearchState>(TSearchState searcher)
        where TSearchState : ILevenshtomatonExecutionState<TSearchState>
        => new SearchEnumerable<TSearchState>(this, searcher);

    private class SearchEnumerable<TSearchState> : IEnumerable<LevenshtrieSearchResult<T>>
        where TSearchState : ILevenshtomatonExecutionState<TSearchState>
    {
        private readonly LevenshtrieCore<T, TCaseSensitivity, TCursor> _trie;
        private readonly TSearchState _initialState;

        public SearchEnumerable(LevenshtrieCore<T, TCaseSensitivity, TCursor> trie, TSearchState initialState)
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
        private readonly LevenshtrieCore<T, TCaseSensitivity, TCursor> _trie;
        private readonly Stack<(int entryIndex, TSearchState searchState)> _state = new();
        private int _cursorDistance;
        private LevenshtrieSearchKind _cursorSearchKind;
        private int _cursorMetadata;
        private TCursor _cursor;

        public SearchEnumerator(LevenshtrieCore<T, TCaseSensitivity, TCursor> trie, TSearchState initialState)
        {
            _trie = trie;
            _state.Push((entryIndex: 0, searchState: initialState));
            _cursor = trie.CreateCursor(NoIndex);
        }

        public LevenshtrieSearchResult<T> Current { get; private set; }

        object System.Collections.IEnumerator.Current => Current;

        public void Dispose() { }

        public bool MoveNext()
        {
        restart:
            if (_cursor.MoveNext(out var result))
            {
                Current = new LevenshtrieSearchResult<T>(result, _cursorDistance, _cursorSearchKind, _cursorMetadata);
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
                    _cursorSearchKind = searchState.TryGetPrefixSearchMetadata(out var metadata) ? LevenshtrieSearchKind.Prefix : LevenshtrieSearchKind.Full;
                    _cursorMetadata = metadata;
                    _cursor = _trie.CreateCursor(entry.ResultIndex);
                    goto restart;
                }
            }

            Current = default;
            return false;
        }

        public void Reset() => throw new NotSupportedException();
    }

    protected ref LevenshtrieCoreEntry GetOrAddEntryRef(ReadOnlySpan<char> key)
    {
        EnsureEntriesHasEmptySlots(2);

        var entries = _entries;
        ref var entry = ref _entries[0];

        while (key.Length > 0)
        {
            Rune.DecodeFromUtf16(key, out var nextRune, out var charsConsumed);
            key = key[charsConsumed..];

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
                        while (tailData.Length > 0 && key.Length > 0)
                        {
                            Rune.DecodeFromUtf16(key, out nextRune, out charsConsumed);
                            key = key[charsConsumed..];

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
                            return ref GetNewBranchEntryRef(ref childEntry, charsMatched, null, []);
                        }
                        else
                        {
                            // The new node branches off at some point along this head / tail data
                            return ref GetNewBranchEntryRef(ref childEntry, charsMatched, nextRune, key);
                        }
                    }

                    break;
                }

                nextChildEntryIndex = childEntry.NextSiblingEntryIndex;
            }

            if (!found)
            {
                return ref GetNewBranchEntryRef(
                    ref entry,
                    entry.EntryValue.Utf16SequenceLength + entry.TailDataLength,
                    nextRune,
                    key);
            }
        }

        return ref entry;
    }

    private ref LevenshtrieCoreEntry GetNewBranchEntryRef(
        ref LevenshtrieCoreEntry parentEntry,
        int splitParentEntryChars,
        Rune? newBranchRune,
        ReadOnlySpan<char> newBranchTailData)
    {
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
            _entries[_entriesSize] = new LevenshtrieCoreEntry
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

            _entries[_entriesSize] = new LevenshtrieCoreEntry
            {
                EntryValue = newBranchRune.Value,
                FirstChildEntryIndex = NoIndex,
                NextSiblingEntryIndex = NoIndex,
                ResultIndex = NoIndex,
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

            return ref _entries[_entriesSize++];
        }
        else
        {
            Debug.Assert(parentEntry.ResultIndex == NoIndex);
            parentEntry.ResultIndex = NoIndex;
            return ref parentEntry;
        }
    }

    private void EnsureEntriesHasEmptySlots(int spacesRequired)
    {
        while (_entries.Length - _entriesSize < spacesRequired)
        {
            Array.Resize(ref _entries, NextArraySize(_entries.Length));
        }
    }

    private void EnsureTailDataHasEmptySlots(int spacesRequired)
    {
        while (_tailData.Length - _tailDataSize < spacesRequired)
        {
            Array.Resize(ref _tailData, NextArraySize(_tailData.Length));
        }
    }

    protected static int NextArraySize(int currentSize) =>
        currentSize switch
        {
            < 16 => 16,
            _ => currentSize * 2
        };
}

[DebuggerDisplay("{EntryValue}")]
internal struct LevenshtrieCoreEntry
{
    public Rune EntryValue;
    public int TailDataIndex;
    public int TailDataLength;
    public int FirstChildEntryIndex;
    public int NextSiblingEntryIndex;
    public int ResultIndex;
}

internal interface ILevenshtrieCursor<T>
{
    bool MoveNext([MaybeNullWhen(false)] out T value);
}
