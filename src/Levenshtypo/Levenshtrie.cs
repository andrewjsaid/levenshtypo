using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace Levenshtypo;

public static class Levenshtrie
{
    /// <summary>
    /// Builds a tree from the given associations between strings and values.
    /// </summary>
    public static Levenshtrie<T> Create<T>(IEnumerable<KeyValuePair<string, T>> source, bool ignoreCase = false)
        => Levenshtrie<T>.Create(source, ignoreCase);

    /// <summary>
    /// Builds a tree from the given strings.
    /// </summary>
    public static Levenshtrie<string> CreateStrings(IEnumerable<string> source, bool ignoreCase = false)
        => Levenshtrie<string>.Create(source.Select(s => new KeyValuePair<string, string>(s, s)), ignoreCase);

}

/// <summary>
/// A data structure capable of associating strings with values and fuzzy lookups on those strings.
/// Supports a single value per unique input string.
/// Does not support modification after creation.
/// </summary>
public abstract class Levenshtrie<T> : ILevenshtomatonExecutor<LevenshtrieSearchResult<T>[]>, ILevenshtomatonExecutor<IEnumerable<LevenshtrieSearchResult<T>>>
{

    private protected Levenshtrie() { }

    private protected abstract bool IgnoreCase { get; }

    /// <summary>
    /// Builds a tree from the given associations between strings and values.
    /// </summary>
    public static Levenshtrie<T> Create(IEnumerable<KeyValuePair<string, T>> source, bool ignoreCase = false)
    {
        if (ignoreCase)
        {
            return Levenshtrie<T, CaseInsensitive>.Create(source);
        }
        else
        {
            return Levenshtrie<T, CaseSensitive>.Create(source);
        }
    }

    /// <summary>
    /// Finds the value associated with the specified key.
    /// </summary>
    public abstract bool TryGetValue(string key, [MaybeNullWhen(false)] out T value);

    /// <summary>
    /// Searches for values with a key at the maximum error distance.
    /// The results are return in an arbitrary order.
    /// </summary>
    public LevenshtrieSearchResult<T>[] Search(string text, int maxEditDistance, LevenshtypoMetric metric = LevenshtypoMetric.Levenshtein)
    {
        var automaton = LevenshtomatonFactory.Instance.Construct(text, maxEditDistance, ignoreCase: IgnoreCase, metric: metric);
        return Search(automaton);
    }

    /// <summary>
    /// Searches for values with a key which is accepted by the specified automaton.
    /// The results are return in an arbitrary order.
    /// </summary>
    public LevenshtrieSearchResult<T>[] Search(Levenshtomaton automaton)
    {
        if (automaton.IgnoreCase != IgnoreCase)
        {
            throw new ArgumentException("Case sensitivity of automaton does not match.");
        }

        ILevenshtomatonExecutor<LevenshtrieSearchResult<T>[]> @this = this;
        return automaton.Execute(@this);
    }

    /// <summary>
    /// Searches for values with a key accepted by the specified search state.
    /// The results are return in an arbitrary order.
    /// </summary>
    public LevenshtrieSearchResult<T>[] Search(LevenshtomatonExecutionState searcher)
        => Search<LevenshtomatonExecutionState>(searcher);

    /// <summary>
    /// Searches for values with a key accepted by the specified search state.
    /// The results are return in an arbitrary order.
    /// </summary>
    public abstract LevenshtrieSearchResult<T>[] Search<TSearchState>(TSearchState searcher)
        where TSearchState : ILevenshtomatonExecutionState<TSearchState>;

    LevenshtrieSearchResult<T>[] ILevenshtomatonExecutor<LevenshtrieSearchResult<T>[]>.ExecuteAutomaton<TSearchState>(TSearchState executionState) => Search(executionState);

    /// <summary>
    /// Lazily searches for values with a key at the maximum error distance.
    /// The results are return in an arbitrary order.
    /// </summary>
    /// <remarks>
    /// Due to lazy evaluation, <see cref="EnumerateSearch"/> uses less
    /// memory than <see cref="Search"/>, and can be faster if not
    /// all results are consumed. However, it is slower when most results
    /// will be retrieved anyway.
    /// </remarks>
    public IEnumerable<LevenshtrieSearchResult<T>> EnumerateSearch(string text, int maxEditDistance, LevenshtypoMetric metric = LevenshtypoMetric.Levenshtein)
    {
        var automaton = LevenshtomatonFactory.Instance.Construct(text, maxEditDistance, ignoreCase: IgnoreCase, metric: metric);
        return EnumerateSearch(automaton);
    }

    /// <summary>
    /// Lazily searches for values with a key which is accepted by the specified automaton.
    /// The results are return in an arbitrary order.
    /// </summary>
    /// <remarks>
    /// Due to lazy evaluation, <see cref="EnumerateSearch"/> uses less
    /// memory than <see cref="Search"/>, and can be faster if not
    /// all results are consumed. However, it is slower when most results
    /// will be retrieved anyway.
    /// </remarks>
    public IEnumerable<LevenshtrieSearchResult<T>> EnumerateSearch(Levenshtomaton automaton)
    {
        if (automaton.IgnoreCase != IgnoreCase)
        {
            throw new ArgumentException("Case sensitivity of automaton does not match.");
        }

        ILevenshtomatonExecutor<IEnumerable<LevenshtrieSearchResult<T>>> @this = this;
        return automaton.Execute(@this);
    }

    /// <summary>
    /// Lazily searches for values with a key accepted by the specified search state.
    /// The results are return in an arbitrary order.
    /// </summary>
    /// <remarks>
    /// Due to lazy evaluation, <see cref="EnumerateSearch"/> uses less
    /// memory than <see cref="Search"/>, and can be faster if not
    /// all results are consumed. However, it is slower when most results
    /// will be retrieved anyway.
    /// </remarks>
    public IEnumerable<LevenshtrieSearchResult<T>> EnumerateSearch(LevenshtomatonExecutionState searcher)
        => EnumerateSearch<LevenshtomatonExecutionState>(searcher);

    /// <summary>
    /// Lazily searches for values with a key accepted by the specified search state.
    /// The results are return in an arbitrary order.
    /// </summary>
    /// <remarks>
    /// Due to lazy evaluation, <see cref="EnumerateSearch"/> uses less
    /// memory than <see cref="Search"/>, and can be faster if not
    /// all results are consumed. However, it is slower when most results
    /// will be retrieved anyway.
    /// </remarks>
    public abstract IEnumerable<LevenshtrieSearchResult<T>> EnumerateSearch<TSearchState>(TSearchState searcher)
        where TSearchState : ILevenshtomatonExecutionState<TSearchState>;

    IEnumerable<LevenshtrieSearchResult<T>> ILevenshtomatonExecutor<IEnumerable<LevenshtrieSearchResult<T>>>.ExecuteAutomaton<TState>(TState executionState) => EnumerateSearch(executionState);
}

internal sealed class Levenshtrie<T, TCaseSensitivity> :
    Levenshtrie<T>, ILevenshtomatonExecutor<LevenshtrieSearchResult<T>[]> where TCaseSensitivity : struct, ICaseSensitivity<TCaseSensitivity>
{
    private readonly Entry[] _entries;
    private readonly T[] _results;
    private readonly char[] _tailData;

    private Levenshtrie(Entry[] entries, T[] results, char[] tailData)
    {
        _entries = entries;
        _results = results;
        _tailData = tailData;
    }

    private protected override bool IgnoreCase => typeof(TCaseSensitivity) == typeof(CaseInsensitive);

    internal static Levenshtrie<T> Create(IEnumerable<KeyValuePair<string, T>> source)
    {
        var flatSource = source.Select(s => new Kvp(s)).ToArray();

        const int ArbitraryMultiplicationFactor = 5;

        var inputKvps = new Kvp[flatSource.Length];
        for (int i = 0; i < flatSource.Length; i++)
        {
            inputKvps[i].Key = flatSource[i].Key;
            inputKvps[i].Value = flatSource[i].Value;
        }

        var entries = new List<Entry>(flatSource.Length * ArbitraryMultiplicationFactor);
        var results = new List<T>(flatSource.Length);
        var tailData = new List<char>(flatSource.Length * ArbitraryMultiplicationFactor);
        var processQueue = new Queue<(Rune nodeValue, int groupIndex, int groupLength, int toEntryIndex)>();

        Array.Sort(flatSource, (a, b) => default(TCaseSensitivity).KeyComparer.Compare(a.Key, b.Key));

        if (flatSource.Length > 0)
        {
            entries.Add(new Entry());
            processQueue.Enqueue((Rune.ReplacementChar, 0, flatSource.Length, 0));
            while (processQueue.Count > 0)
            {
                var (nodeValue, groupIndex, groupLength, toEntryIndex) = processQueue.Dequeue();
                AppendChildren(nodeValue, groupIndex, groupLength, toEntryIndex);
            }
        }
        else
        {
            entries.Add(new Entry
            {
                EntryValue = Rune.ReplacementChar,
                ResultIndex = -1
            });
        }

        return new Levenshtrie<T, TCaseSensitivity>(entries.ToArray(), results.ToArray(), tailData.ToArray());

        void AppendChildren(Rune nodeValue, int groupIndex, int groupLength, int toEntryIndex)
        {
            var group = flatSource.AsSpan(groupIndex, groupLength);

            int resultIndex = -1;
            var tailDataIndex = tailData.Count;

            if (group.Length == 1
                && group[0].NextReadIndex != 0
                && group[0].Key.Length - group[0].NextReadIndex > 0)
            {
                resultIndex = results.Count;
                results.Add(group[0].Value);

                var entryTailData = group[0].Key.AsSpan(group[0].NextReadIndex);

                // In .NET 9 we can use alternate lookup to re-use tail data.

#if NET8_0_OR_GREATER
                tailData.AddRange(entryTailData);
#else
                tailData.AddRange(entryTailData.ToArray());
#endif

                entries[toEntryIndex] = new Entry
                {
                    EntryValue = nodeValue,
                    ChildEntriesStartIndex = 0,
                    NumChildren = 0,
                    ResultIndex = resultIndex,
                    TailDataIndex = tailDataIndex,
                    TailDataLength = entryTailData.Length
                };

                return;
            }

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
                        if (resultIndex != -1)
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

            } while (resultIndex == -1 && childGroups.Count == 1 && firstItemNextReadIndex > 0);

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

            foreach (var (entryDiscriminator, rangeStartIndex, rangeEndIndexExcl) in childGroups)
            {
                processQueue.Enqueue((entryDiscriminator, groupIndex + rangeStartIndex, rangeEndIndexExcl - rangeStartIndex, entries.Count));
                entries.Add(new Entry());
            }

            entries[toEntryIndex] = new Entry
            {
                EntryValue = nodeValue,
                ResultIndex = resultIndex,
                ChildEntriesStartIndex = childGroups.Count == 0 ? 0 : childEntriesStartIndex,
                NumChildren = childGroups.Count,
                TailDataIndex = tailDataIndex,
                TailDataLength = tailDataLength
            };
        }
    }

    public override bool TryGetValue(string key, [MaybeNullWhen(false)] out T value)
    {
        var entries = _entries;

        var entry = _entries[0];
        var keySpan = key.AsSpan();

        while (keySpan.Length > 0)
        {
            Rune.DecodeFromUtf16(keySpan, out var nextRune, out var charsConsumed);
            keySpan = keySpan[charsConsumed..];

            var found = false;
            var childEntries = entries.AsSpan(entry.ChildEntriesStartIndex, entry.NumChildren);
            for (int i = 0; i < childEntries.Length; i++)
            {
                if (default(TCaseSensitivity).Equals(childEntries[i].EntryValue, nextRune))
                {
                    found = true;
                    entry = childEntries[i];

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
            var entry = _entries[entryIndex];

            if (searchState.IsFinal && entry.ResultIndex >= 0)
            {
                results.Add(new LevenshtrieSearchResult<T>(searchState.Distance, _results[entry.ResultIndex]));
            }

            var childEntries = _entries.AsSpan(entry.ChildEntriesStartIndex, entry.NumChildren);
            for (int i = 0; i < childEntries.Length; i++)
            {
                var childEntry = childEntries[i];
                if (searchState.MoveNext(childEntry.EntryValue, out var nextSearchState))
                {
                    bool matchesTailData = true;
                    var entryTailDataLength = childEntry.TailDataLength;
                    if (entryTailDataLength > 0)
                    {
                        var tailData = _tailData.AsSpan(childEntry.TailDataIndex, entryTailDataLength);

                        foreach (var tailDataRune in tailData.EnumerateRunes())
                        {
                            if (!nextSearchState.MoveNext(tailDataRune, out nextSearchState))
                            {
                                matchesTailData = false;
                                break;
                            }
                        }

                    }

                    if (matchesTailData)
                    {
                        if (depthLeft > 0)
                        {
                            TraverseChildrenOf(entry.ChildEntriesStartIndex + i, nextSearchState, depthLeft - 1);
                        }
                        else
                        {
                            processQueue ??= new();
                            processQueue.Enqueue((entry.ChildEntriesStartIndex + i, nextSearchState));
                        }
                    }
                }
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
        private readonly Stack<(int entryIndex, TSearchState searchState, int nextChildIndex)> _state = new();
        
        private const int CheckCurrentChildIndex = -1;

        public SearchEnumerator(Levenshtrie<T, TCaseSensitivity> trie, TSearchState initialState)
        {
            _trie = trie;
            _state.Push((entryIndex: 0, searchState: initialState, nextChildIndex: CheckCurrentChildIndex));
        }

        public LevenshtrieSearchResult<T> Current { get; private set; }

        object IEnumerator.Current => Current;

        public void Dispose() { }

        public bool MoveNext()
        {
            while (_state.TryPop(out var state))
            {
                var (entryIndex, searchState, nextChildIndex) = state;

                var entry = _trie._entries[entryIndex];

                if (nextChildIndex == CheckCurrentChildIndex)
                {
                    if (searchState.IsFinal && entry.ResultIndex >= 0)
                    {
                        if (entry.NumChildren > 0)
                        {
                            _state.Push((entryIndex: entryIndex, searchState: searchState, nextChildIndex: 0));
                        }
                        Current = new LevenshtrieSearchResult<T>(searchState.Distance, _trie._results[entry.ResultIndex]);
                        return true;
                    }
                    
                    // Just increment this index instead of Push/Pop
                    nextChildIndex = 0;
                }

                if (nextChildIndex < entry.NumChildren)
                {
                    if (nextChildIndex < entry.NumChildren - 1)
                    {
                        _state.Push((entryIndex: entryIndex, searchState: searchState, nextChildIndex: nextChildIndex + 1));
                    }

                    var childEntry = _trie._entries[entry.ChildEntriesStartIndex + nextChildIndex];

                    if (searchState.MoveNext(childEntry.EntryValue, out var nextSearchState))
                    {
                        bool matchesTailData = true;
                        var entryTailDataLength = childEntry.TailDataLength;
                        if (entryTailDataLength > 0)
                        {
                            var tailData = _trie._tailData.AsSpan(childEntry.TailDataIndex, entryTailDataLength);

                            foreach (var tailDataRune in tailData.EnumerateRunes())
                            {
                                if (!nextSearchState.MoveNext(tailDataRune, out nextSearchState))
                                {
                                    matchesTailData = false;
                                    break;
                                }
                            }

                        }

                        if (matchesTailData)
                        {
                            _state.Push((entryIndex: entry.ChildEntriesStartIndex + nextChildIndex, searchState: nextSearchState, nextChildIndex: CheckCurrentChildIndex));
                        }
                    }
                }
            }

            Current = default;
            return false;
        }

        public void Reset() => throw new NotSupportedException();
    }

    private struct Kvp
    {
        public Kvp(KeyValuePair<string, T> input)
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
        public int ChildEntriesStartIndex;
        public int NumChildren;
        public int ResultIndex;
    }
}
