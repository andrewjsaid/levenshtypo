using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Levenshtypo;

public sealed class Levenshtrie<T>
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

    public static Levenshtrie<T> Create(IEnumerable<KeyValuePair<string, T>> input)
    {
        var entries = new List<Entry>();
        var results = new List<T>();
        var tailData = new List<char>();

        entries.Add(new());
        var rootEntry = AppendChildren('\0', input.ToArray(), nextDiscriminatorIndex: 0);
        entries[0] = rootEntry;

        return new Levenshtrie<T>(entries.ToArray(), results.ToArray(), tailData.ToArray());

        Entry AppendChildren(char nodeValue, IReadOnlyList<KeyValuePair<string, T>> group, int nextDiscriminatorIndex)
        {
            int resultIndex = -1;

            const int NumCharsForDirectComparison = 2;
            if (nextDiscriminatorIndex != 0 && group.Count == 1 && group[0].Key.Length - nextDiscriminatorIndex >= NumCharsForDirectComparison)
            {
                resultIndex = results.Count;
                results.Add(group[0].Value);

                var tailDataIndex = tailData.Count;
                var entryTailData = group[0].Key.AsSpan(nextDiscriminatorIndex);
                tailData.AddRange(entryTailData);

                return new Entry
                {
                    EntryValue = nodeValue,
                    ChildEntriesStartIndex = 0,
                    NumChildren = 0,
                    ResultIndex = resultIndex,
                    TailDataIndex = tailDataIndex,
                    TailDataLength = entryTailData.Length
                };
            }

            var childNodes = new SortedDictionary<ushort, List<KeyValuePair<string, T>>>();

            foreach (var item in group)
            {
                if (item.Key.Length == nextDiscriminatorIndex)
                {
                    resultIndex = results.Count;
                    results.Add(item.Value);
                }
                else
                {
                    var nextKey = item.Key[nextDiscriminatorIndex];
                    if (!childNodes.TryGetValue(nextKey, out var targetList))
                    {
                        childNodes.Add(nextKey, targetList = new());
                    }
                    targetList.Add(item);
                }
            }

            int childEntriesStartIndex = entries.Count;

            var orderedChildNodes = childNodes.OrderBy(x => (int)x.Key).ToList();
            foreach (var entry in childNodes)
            {
                entries.Add(new());
            }

            int writeIndex = childEntriesStartIndex;
            foreach (var entry in childNodes)
            {
                entries[writeIndex++] = AppendChildren((char)entry.Key, entry.Value, nextDiscriminatorIndex + 1);
            }


            return new Entry
            {
                EntryValue = nodeValue,
                ChildEntriesStartIndex = childNodes.Count == 0 ? 0 : childEntriesStartIndex,
                ResultIndex = resultIndex,
                NumChildren = childNodes.Count
            };
        }
    }

    public bool TryGetValue(string key, [MaybeNullWhen(false)] out T value)
    {
        var entries = _entries;

        var entry = _entries[0];
        var keyIndex = 0;

        while (keyIndex < key.Length)
        {
            var nextChar = key[keyIndex++];

            var found = false;
            var childEntries = entries.AsSpan(entry.ChildEntriesStartIndex, entry.NumChildren);
            for (int i = 0; i < childEntries.Length; i++)
            {
                if (childEntries[i].EntryValue == nextChar)
                {
                    found = true;
                    entry = childEntries[i];

                    var entryTailDataLength = entry.TailDataLength;
                    if (entryTailDataLength > 0)
                    {
                        if (keyIndex + entryTailDataLength > key.Length
                            || !_tailData.AsSpan(entry.TailDataIndex, entryTailDataLength).SequenceEqual(key.AsSpan(keyIndex, entryTailDataLength)))
                        {
                            found = false;
                        }

                        keyIndex += entry.TailDataLength;
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

    public IReadOnlyList<T> Search(Levenshtomaton automaton)
    {
        var results = new HashSet<T>();
        TraverseChildrenOf(0, automaton.Start());
        return results.ToArray();

        void TraverseChildrenOf(int entryIndex, Levenshtomaton.State enumerator)
        {
            var entry = _entries[entryIndex];
            var childEntries = _entries.AsSpan(entry.ChildEntriesStartIndex, entry.NumChildren);
            for (int i = 0; i < childEntries.Length; i++)
            {
                var childEntry = childEntries[i];
                if (enumerator.MoveNext(childEntry.EntryValue, out var nextEnumerator))
                {
                    bool matchesTailData = true;
                    var entryTailDataLength = childEntry.TailDataLength;
                    if (entryTailDataLength > 0)
                    {
                        var tailData = _tailData.AsSpan(childEntry.TailDataIndex, entryTailDataLength);
                        for (int tailDataIndex = 0; tailDataIndex < tailData.Length; tailDataIndex++)
                        {
                            if (!nextEnumerator.MoveNext(tailData[tailDataIndex], out nextEnumerator))
                            {
                                matchesTailData = false;
                                break;
                            }
                        }
                    }

                    if (matchesTailData)
                    {
                        if (nextEnumerator.IsFinal && childEntry.ResultIndex is >= 0)
                        {
                            results.Add(_results[childEntry.ResultIndex]);
                        }

                        TraverseChildrenOf(entry.ChildEntriesStartIndex + i, nextEnumerator);
                    }
                }
            }
        }
    }

    [DebuggerDisplay("{EntryValue}")]
    public readonly struct Entry
    {
        public readonly char EntryValue { get; init; }
        public readonly int TailDataIndex { get; init; }
        public readonly int TailDataLength { get; init; }
        public readonly int ChildEntriesStartIndex { get; init; }
        public readonly int NumChildren { get; init; }
        public readonly int ResultIndex { get; init; }
    }

}
