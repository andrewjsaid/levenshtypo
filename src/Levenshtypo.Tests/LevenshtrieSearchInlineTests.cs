using Shouldly;

namespace Levenshtypo.Tests;

public class LevenshtrieSearchInlineTests
{
    [Fact]
    public void Food()
    {
        string[] entries = ["f", "food", "good", "mood", "flood", "fod", "fob", "foodie"];
        var t = Levenshtrie<string>.Create(entries.Select(e => new KeyValuePair<string, string>(e, e)));

        t.Search(new LevenshtomatonFactory().Construct("food", 0))
            .ShouldBe(["food"], ignoreOrder: true);

        t.Search(new LevenshtomatonFactory().Construct("food", 1))
            .ShouldBe(["food", "good", "mood", "flood", "fod"], ignoreOrder: true);

        t.Search(new LevenshtomatonFactory().Construct("food", 2))
            .ShouldBe(["food", "good", "mood", "flood", "fod", "fob", "foodie"], ignoreOrder: true);
    }
}
