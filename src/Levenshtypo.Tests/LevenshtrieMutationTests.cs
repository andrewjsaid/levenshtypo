using Shouldly;

namespace Levenshtypo.Tests;

public class LevenshtrieMutationTests
{
    [Fact]
    public void GeneratedTests_Guids()
    {
        RunTest(Enumerable.Range(0, 100_000).Select(_ => Guid.NewGuid().ToString()));
    }

    [Fact]
    public void GeneratedTests_Integers()
    {
        RunTest(Enumerable.Range(0, 100_000).Select(i => i.ToString()));
    }

    [Fact]
    public void HandWrittenTests()
    {
        RunTest([
            "a",
            "abcde", // extend head
            "abcdefghi", // extend tail data
            "az", // branch off head
            "abz", // branch off tail data
            "abc", // stop at head
            "abcdefg", // stop at tail data
            "",
            "z" // branch off root
            ]);
    }

    [Fact]
    public void GeneratedTests_Guids_MultiMap()
    {
        RunMultiMapTest(Enumerable.Range(0, 100_000).Select(_ => Guid.NewGuid().ToString()));
    }

    [Fact]
    public void GeneratedTests_Integers_MultiMap()
    {
        RunMultiMapTest(Enumerable.Range(0, 100_000).Select(i => i.ToString()));
    }

    [Fact]
    public void HandWrittenTests_MultiMap()
    {
        RunMultiMapTest([
            "a",
            "abcde", // extend head
            "abcdefghi", // extend tail data
            "az", // branch off head
            "abz", // branch off tail data
            "abc", // stop at head
            "abcdefg", // stop at tail data
            "",
            "z" // branch off root
            ]);
    }

    private void RunTest(IEnumerable<string> keys)
    {
        var trie = Levenshtrie.CreateEmpty<string>();

        var addedKeys = new List<string>();

        for (int i = 0; i < 2; i++)
        {
            addedKeys.Clear();

            foreach (var key in keys)
            {
                addedKeys.Add(key);

                trie.TryGetValue(key, out _).ShouldBeFalse();

                trie.Add(key, key);

                trie.TryGetValue(key, out var found).ShouldBeTrue();
                ReferenceEquals(key, found).ShouldBeTrue();
            }

            foreach (var key in addedKeys)
            {
                trie.TryGetValue(key, out var found).ShouldBeTrue();
                ReferenceEquals(key, found).ShouldBeTrue();
            }

            foreach (var key in addedKeys)
            {
                trie.Remove(key);

                trie.TryGetValue(key, out var found).ShouldBeFalse();
                found.ShouldBeNull();
            }
        }
    }


    private void RunMultiMapTest(IEnumerable<string> keys)
    {
        var trie = Levenshtrie.CreateEmptyMulti<string>();

        var addedKeys = new List<string>();

        for (int i = 0; i < 2; i++)
        {
            addedKeys.Clear();

            foreach (var key in keys)
            {
                addedKeys.Add(key);

                trie.GetValues(key).ToArray().ShouldBeEmpty();

                trie.Add(key, key + "_1");

                trie.GetValues(key).ToArray().ShouldBe([key + "_1"], comparer: StringComparer.Ordinal);

                ref var two = ref trie.GetOrAdd(key, key + "_2", StringComparer.Ordinal, out var twoExists);
                twoExists.ShouldBeFalse();
                two.ShouldBe(key + "_2");

                var three1 = key + "_3";
                trie.Add(key, three1);

                var three2 = string.Create(three1.Length, three1, static (span, s) => s.CopyTo(span));
                object.ReferenceEquals(three1, three2).ShouldBeFalse();
                ref var three = ref trie.GetOrAdd(key, three2, StringComparer.Ordinal, out var threeExists);
                threeExists.ShouldBeTrue();
                object.ReferenceEquals(three1, three).ShouldBeTrue();

                trie.GetValues(key).ToArray().ShouldBe([key + "_1", key + "_2", key + "_3"], comparer: StringComparer.Ordinal);
            }

            foreach (var key in addedKeys)
            {
                trie.GetValues(key).ToArray().ShouldBe([key + "_1", key + "_2", key + "_3"], comparer: StringComparer.Ordinal);
            }

            foreach (var key in addedKeys)
            {
                var removed = trie.Remove(key, key + "_2", StringComparer.Ordinal);
                removed.ShouldBeTrue();

                trie.GetValues(key).ToArray().ShouldBe([key + "_1", key + "_3"], comparer: StringComparer.Ordinal);

                removed = trie.RemoveAll(key);
                removed.ShouldBeTrue();

                trie.GetValues(key).ToArray().ShouldBeEmpty();
            }
        }
    }

}
