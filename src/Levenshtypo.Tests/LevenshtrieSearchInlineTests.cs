using Shouldly;

namespace Levenshtypo.Tests;

public class LevenshtrieSearchInlineTests
{
    [Fact]
    public void Food()
    {
        string[] entries = ["f", "food", "good", "mood", "flood", "fod", "fob", "foodie", "\U0002f971"];
        var t = Levenshtrie<string>.Create(entries.Select(e => new KeyValuePair<string, string>(e, e)));

        t.Search(new LevenshtomatonFactory().Construct("food", 0))
            .ShouldBe(["food"], ignoreOrder: true);

        t.Search(new LevenshtomatonFactory().Construct("food", 1))
            .ShouldBe(["food", "good", "mood", "flood", "fod"], ignoreOrder: true);

        t.Search(new LevenshtomatonFactory().Construct("food", 2))
            .ShouldBe(["food", "good", "mood", "flood", "fod", "fob", "foodie"], ignoreOrder: true);

        t.Search(new LevenshtomatonFactory().Construct("\U0001f970", 1))
            .ShouldBe(["f", "\U0002f971"], ignoreOrder: true);
    }
}
