using Shouldly;

namespace Levenshtypo.Tests;

public class LevenshtrieCreationTests
{
    [Fact]
    public void Levenshtrie_DuplicateEntries_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            Levenshtrie<int>.Create([
                new KeyValuePair<string, int>("one", 1),
                new KeyValuePair<string, int>("one", 1),
                ]);
        });
    }

    [Fact]
    public void LevenshtrieMultiMap_DuplicateEntries_Allowed()
    {
        var mm = LevenshtrieMultiMap<int>.Create([
            new KeyValuePair<string, int>("one", 1),
            new KeyValuePair<string, int>("one", 2),
        ]);

        mm.GetValues("one").ShouldBe([1, 2]);
    }
}
