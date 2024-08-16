using Shouldly;

namespace Levenshtypo.Tests;

public class LevenshtomatonFactoryTests
{

    [Fact]
    public void FactoryCreatesCorrect()
    {
        var factory = new LevenshtomatonFactory();
        foreach (var metric in (ReadOnlySpan<LevenshtypoMetric>)[LevenshtypoMetric.Levenshtein, LevenshtypoMetric.RestrictedEdit])
        {
            foreach (var ignoreCase in (ReadOnlySpan<bool>)[false, true])
            {
                for (int i = 0; i < 100; i++)
                {
                    var text = $"{metric}_{ignoreCase}_{i}";

                    var automaton = factory.Construct(
                        text,
                        maxEditDistance: i,
                        ignoreCase: ignoreCase,
                        metric: metric);

                    automaton.Text.ShouldBe(text, text);
                    automaton.MaxEditDistance.ShouldBe(i, text);
                    automaton.IgnoreCase.ShouldBe(ignoreCase, text);
                    automaton.Metric.ShouldBe(metric, text);
                }
            }
        }
    }
}
