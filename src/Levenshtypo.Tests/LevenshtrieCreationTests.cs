using Levenshtypo;

namespace Levenshtypo.Tests;

public class LevenshtrieCreationTests
{
    [Fact]
    public void DuplicateEntries()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            Levenshtrie<int>.Create([
                new KeyValuePair<string, int>("one", 1),
                new KeyValuePair<string, int>("one", 1),
                ]);
        });
    }
}
