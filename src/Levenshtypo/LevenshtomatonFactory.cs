using System;
using System.Collections.Concurrent;

namespace Levenshtypo;

/// <summary>
/// Entry point for creating Levenshtomatons.
/// It is intended to have a single one per application since the internal cache is 
/// basically a constant State Machine.
/// </summary>
public sealed class LevenshtomatonFactory
{
    /// <summary>
    /// Automatons with Levenshtein distance greater than this value are not supported.
    /// </summary>
    public const int MaxEditDistance = 3;

    private readonly ConcurrentDictionary<TemplateKey, ParameterizedLevenshtomaton.Template> _templates = new ();

    internal LevenshtomatonFactory() { }

    public static LevenshtomatonFactory Instance { get; } = new ();

    /// <summary>
    /// Construct a <see cref="Levenshtomaton"/> which accepts only strings
    /// with maximum edit distance from <see cref="s"/>.
    /// </summary>
    /// <param name="s">The string against which others will be compared.</param>
    /// <param name="maxEditDistance">The inclusive maximum edit distance for allowed strings.</param>
    /// <param name="ignoreCase">When true then the automaton will ignore casing differences.</param>
    /// <param name="metric">The metric.</param>
    public Levenshtomaton Construct(string s, int maxEditDistance, bool ignoreCase = false, LevenshtypoMetric metric = LevenshtypoMetric.Levenshtein)
    {
        if (maxEditDistance is > MaxEditDistance or < 0)
        {
            // The limitation is purely for practical purposes as the number of states can truly explode.
            throw new ArgumentOutOfRangeException(nameof(maxEditDistance));
        }

        switch (maxEditDistance, metric, ignoreCase)
        {
            case (0, _, false):
                return new Distance0Levenshtomaton<CaseSensitive>(s, metric);

            case (0, _, true):
                return new Distance0Levenshtomaton<CaseInsensitive>(s, metric);

            case (1, LevenshtypoMetric.Levenshtein, false):
                return new Distance1LevenshteinLevenshtomaton<CaseSensitive>(s);

            case (1, LevenshtypoMetric.Levenshtein, true):
                return new Distance1LevenshteinLevenshtomaton<CaseInsensitive>(s);

            case (1, LevenshtypoMetric.RestrictedEdit, false):
                return new Distance1RestrictedEditLevenshtomaton<CaseSensitive>(s);

            case (1, LevenshtypoMetric.RestrictedEdit, true):
                return new Distance1RestrictedEditLevenshtomaton<CaseInsensitive>(s);

            case (2, LevenshtypoMetric.Levenshtein, false):
                return new Distance2LevenshteinLevenshtomaton<CaseSensitive>(s);

            case (2, LevenshtypoMetric.Levenshtein, true):
                return new Distance2LevenshteinLevenshtomaton<CaseInsensitive>(s);

            case (2, LevenshtypoMetric.RestrictedEdit, false):
                return new Distance2RestrictedEditLevenshtomaton<CaseSensitive>(s);

            case (2, LevenshtypoMetric.RestrictedEdit, true):
                return new Distance2RestrictedEditLevenshtomaton<CaseInsensitive>(s);

            default:
                var template = _templates.GetOrAdd(new(maxEditDistance, metric), key => ParameterizedLevenshtomaton.CreateTemplate(key.MaxEditDistance, key.Metric));
                return template.Instantiate(s, ignoreCase);
        }
    }

    private record struct TemplateKey(int MaxEditDistance, LevenshtypoMetric Metric);
}