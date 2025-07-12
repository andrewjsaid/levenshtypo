using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace Levenshtypo;

internal interface ILevenshtrieCoreSingle<T>
{
    bool IgnoreCase { get; }

    LevenshtrieSearchResult<T>[] Search<TSearchState>(TSearchState searcher)
        where TSearchState : ILevenshtomatonExecutionState<TSearchState>;

    IEnumerable<LevenshtrieSearchResult<T>> EnumerateSearch<TSearchState>(TSearchState searcher)
        where TSearchState : ILevenshtomatonExecutionState<TSearchState>;

    ref T GetOrAddRef(ReadOnlySpan<char> key, out bool exists);

    bool ContainsKey(ReadOnlySpan<char> key);

    bool TryGetValue(ReadOnlySpan<char> key, [MaybeNullWhen(false)] out T value);

    void Add(ReadOnlySpan<char> key, T value);

    bool Remove(ReadOnlySpan<char> key);
}

internal sealed class LevenshtrieCoreSingle<T, TCaseSensitivity>
    : LevenshtrieCore<T, TCaseSensitivity, LevenshtrieCoreSingleCursor<T>>,
    ILevenshtrieCoreSingle<T>
    where TCaseSensitivity : struct, ICaseSensitivity<TCaseSensitivity>
{
    private T[] _results = [];
    private int _resultsSize;
    
    private LevenshtrieCoreSingle(LevenshtrieCoreEntry root) : base(root) { }

    internal static ILevenshtrieCoreSingle<T> Create(IEnumerable<KeyValuePair<string, T>> source)
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

        var trie = new LevenshtrieCoreSingle<T, TCaseSensitivity>(root);

        foreach (var (key, value) in source)
        {
            trie.Add(key, value);
        }

        return trie;
    }

    public bool IgnoreCase => typeof(TCaseSensitivity) == typeof(CaseInsensitive);

    public ref T GetOrAddRef(ReadOnlySpan<char> key, out bool exists)
    {
        ref var entry = ref GetOrAddEntryRef(key);

        if (entry.ResultIndex is NoIndex)
        {
            // Write result to results array
            var newResultIndex = entry.ResultIndex = TakeResultSlot();

            exists = false;

            ref var result = ref _results[newResultIndex];
            result = default!;
            return ref result!;
        }
        else
        {
            exists = true;
            return ref _results[entry.ResultIndex];
        }
    }

    public bool ContainsKey(ReadOnlySpan<char> key)
    {
        ref var entry = ref GetEntryRef(key);

        return !Unsafe.IsNullRef(ref entry) && entry.ResultIndex is not NoIndex;
    }

    public bool TryGetValue(ReadOnlySpan<char> key, [MaybeNullWhen(false)] out T value)
    {
        ref var entry = ref GetEntryRef(key);

        if (Unsafe.IsNullRef(ref entry) || entry.ResultIndex is NoIndex)
        {
            value = default;
            return false;
        }

        value = _results[entry.ResultIndex];
        return true;
    }

    public void Add(ReadOnlySpan<char> key, T value)
    {
        ref var entry = ref GetOrAddEntryRef(key);

        if (entry.ResultIndex is not NoIndex)
        {
            throw new ArgumentException("May not use this data structure with duplicate keys.", nameof(key));
        }

        // Write result to results array
        var newResultIndex = TakeResultSlot();

        _results[newResultIndex] = value;

        entry.ResultIndex = newResultIndex;
    }

    public bool Remove(ReadOnlySpan<char> key)
    {
        ref var entry = ref GetEntryRef(key);

        if (Unsafe.IsNullRef(ref entry) || entry.ResultIndex is NoIndex)
        {
            return false;
        }

        _results[entry.ResultIndex] = default!;
        entry.ResultIndex = NoIndex;

        return true;
    }

    protected override LevenshtrieCoreSingleCursor<T> CreateCursor(int resultIndex)
        => new LevenshtrieCoreSingleCursor<T>(_results, resultIndex);

    private int TakeResultSlot()
    {
        EnsureResultsHasEmptySlots(spacesRequired: 1);
        return _resultsSize++;
    }

    private void EnsureResultsHasEmptySlots(int spacesRequired)
    {
        while (_results.Length - _resultsSize < spacesRequired)
        {
            Array.Resize(ref _results, NextArraySize(_results.Length));
        }
    }
}

internal struct LevenshtrieCoreSingleCursor<T>(T[] results, int index) : ILevenshtrieCursor<T>
{
    private const int NoIndex = -1;

    private readonly T[] _results = results;
    private int _index = index;

    public bool MoveNext([MaybeNullWhen(false)] out T value)
    {
        var index = _index;

        if (index is NoIndex)
        {
            value = default;
            return false;
        }

        value = _results[index];
        _index = NoIndex;
        return true;
    }
}