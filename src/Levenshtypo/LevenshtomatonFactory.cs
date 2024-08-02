using System;
using System.Collections.Concurrent;
using System.Linq;

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
    public Levenshtomaton Construct(string s, int maxEditDistance, bool ignoreCase = false, LevenshtypoMetric metric = LevenshtypoMetric.Levenshtein)
    {
        if (maxEditDistance > MaxEditDistance)
        {
            // The limitation is purely for practical purposes as the number of states can truly explode.
            throw new ArgumentOutOfRangeException(nameof(maxEditDistance));
        }

        if (s.Any(char.IsSurrogate))
        {
            // The limitation is because I haven't had the time to implement otherwise.
            throw new ArgumentException(nameof(s), "Unable to construct a Levenshtomaton for this input.");
        }

        if (maxEditDistance == 0)
        {
            if (ignoreCase)
            {
                return new Distance0Levenshtomaton<CaseInsensitive>(s, metric);
            }
            else
            {
                return new Distance0Levenshtomaton<CaseSensitive>(s, metric);
            }
        }

        var template = _templates.GetOrAdd(new (maxEditDistance, metric), key => ParameterizedLevenshtomaton.CreateTemplate(key.MaxEditDistance, key.Metric));
        return template.Instantiate(s, ignoreCase);
    }

    private record struct TemplateKey(int MaxEditDistance, LevenshtypoMetric Metric);
}