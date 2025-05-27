using Shouldly;

namespace Levenshtypo.Tests;

/// <summary>
/// Tests set semantics of <see cref="LevenshtrieSet{T}"/>
/// </summary>
public class LevenshtrieSetSemanticsTests
{

    [Theory]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(20)]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(10000)]
    public void LevenshtrieSet_should_follow_set_semantics(int count)
    {
        var trie = Levenshtrie.CreateEmptySet<int>();

        // First we warm up with some random data
        for (int i = 0; i < 100; i++)
        {
            trie.ContainsKey(i.ToString()).ShouldBeFalse();

            for (int j = 0; j < 20; j++)
            {
                trie.Contains(i.ToString(), j).ShouldBeFalse();

                trie.Add(i.ToString(), j);

                trie.Contains(i.ToString(), j).ShouldBeTrue();
            }

            trie.ContainsKey(i.ToString()).ShouldBeTrue();
        }

        const string key = "hello";

        var entries = new HashSet<int>();

        for (int i = 0; i < count; i++)
        {
            var mod = i % 3;
            if (mod == 0)
            {
                trie.Add(key, i).ShouldBeTrue();
                trie.Contains(key, i).ShouldBeTrue();

                trie.Add(key, i).ShouldBeFalse();
                trie.Contains(key, i).ShouldBeTrue();

                entries.Add(i);
            }
            else if (mod == 1)
            {
                ref var result = ref trie.GetOrAddRef(key, i, out var exists);
                result.ShouldBe(i);
                exists.ShouldBeFalse();
                trie.Contains(key, i).ShouldBeTrue();

                result = ref trie.GetOrAddRef(key, i - 1, out exists);
                result.ShouldBe(i - 1);
                exists.ShouldBeTrue();

                entries.Add(i);
            }
            else
            {
                trie.Contains(key, i).ShouldBeFalse();
                trie.Remove(key, i).ShouldBeFalse();
            }
        }

        trie.GetValues(key).ShouldBe(entries, ignoreOrder: true);
        trie.Search(key, 0).Select(r => r.Result).ShouldBe(entries, ignoreOrder: true);
        trie.EnumerateSearch(key, 0).Select(r => r.Result).ShouldBe(entries, ignoreOrder: true);

        for (int i = 0; i < count; i++)
        {
            var mod = i % 3;
            if (mod is 0 or 1)
            {
                trie.Contains(key, i).ShouldBeTrue();
            }
            else
            {
                trie.Contains(key, i).ShouldBeFalse();
            }
        }

        for (int i = 0; i < count; i++)
        {
            if (i % 4 == 0)
            {
                trie.Remove(key, i).ShouldBe(i % 3 is 0 or 1);
                trie.Remove(key, i).ShouldBeFalse();

                entries.Remove(i);
            }
        }

        trie.GetValues(key).ShouldBe(entries, ignoreOrder: true);

        trie.RemoveAll(key).ShouldBeTrue();
        trie.RemoveAll(key).ShouldBeFalse();

        trie.GetValues(key).ShouldBeEmpty();
    }


    [Fact]
    public void Create_set_with_all_english_words()
    {
        // This test really pushes the bounds of what is reasonable
        var words = DataHelpers.EnglishWords();
        var trie = Levenshtrie.CreateEmptySet<string>();
        foreach (var word in words)
        {
            trie.Add(string.Empty, word).ShouldBeTrue();
        }

        foreach (var word in words)
        {
            trie.Contains(string.Empty, word).ShouldBeTrue();
        }
    }
}
