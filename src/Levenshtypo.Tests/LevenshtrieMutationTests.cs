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
        

    private void RunTest(IEnumerable<string> keys)
    {
        var trie = Levenshtrie.CreateEmpty<string>();

        var addedKeys = new List<string>();

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
