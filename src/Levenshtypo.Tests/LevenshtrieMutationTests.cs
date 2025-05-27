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
    public void GeneratedTests_Guids_Set()
    {
        RunSetTest(Enumerable.Range(0, 100_000).Select(_ => Guid.NewGuid().ToString()));
    }

    [Fact]
    public void GeneratedTests_Integers_Set()
    {
        RunSetTest(Enumerable.Range(0, 100_000).Select(i => i.ToString()));
    }

    [Fact]
    public void HandWrittenTests_Set()
    {
        RunSetTest([
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


    private void RunSetTest(IEnumerable<string> keys)
    {
        var trie = Levenshtrie.CreateEmptySet<string>(resultComparer: StringComparer.Ordinal);

        var addedKeys = new List<string>();

        for (int i = 0; i < 2; i++)
        {
            addedKeys.Clear();

            foreach (var key in keys)
            {
                addedKeys.Add(key);

                trie.GetValues(key).ToArray().ShouldBeEmpty();

                trie.Add(key, key + "_1").ShouldBeTrue();

                trie.GetValues(key).ToArray().ShouldBe([key + "_1"], comparer: StringComparer.Ordinal);

                ref var two = ref trie.GetOrAddRef(key, key + "_2", out var twoExists);
                twoExists.ShouldBeFalse();
                two.ShouldBe(key + "_2");

                var three1 = key + "_3";
                trie.Add(key, three1).ShouldBeTrue();

                var three2 = string.Create(three1.Length, three1, static (span, s) => s.CopyTo(span));
                object.ReferenceEquals(three1, three2).ShouldBeFalse();
                ref var three = ref trie.GetOrAddRef(key, three2, out var threeExists);
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
                var removed = trie.Remove(key, key + "_2");
                removed.ShouldBeTrue();

                trie.GetValues(key).ToArray().ShouldBe([key + "_1", key + "_3"], comparer: StringComparer.Ordinal);

                removed = trie.RemoveAll(key);
                removed.ShouldBeTrue();

                trie.GetValues(key).ToArray().ShouldBeEmpty();
            }
        }
    }

}
