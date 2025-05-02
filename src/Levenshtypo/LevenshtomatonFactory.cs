using System;

namespace Levenshtypo;

/// <summary>
/// Provides a high-performance factory for constructing <see cref="Levenshtomaton"/> instances,
/// which are deterministic finite automatons (DFAs) optimized for evaluating Levenshtein distances
/// between strings.
///
/// <para>This factory is intended to be used as a singleton via <see cref="Instance"/>.
/// Internally, it caches parameterized state machine templates for performance when constructing
/// automatons with higher edit distances.</para>
///
/// <para>The factory and the automatons it creates are thread-safe and may be used concurrently
/// from multiple threads.</para>
/// </summary>
public sealed class LevenshtomatonFactory
{
    private ParameterizedLevenshtomaton.Template? _d3Levenshtein;
    private ParameterizedLevenshtomaton.Template? _d3RestrictedEdit;

    /// <summary>
    /// Prevents external construction. Use the <see cref="Instance"/> singleton instead.
    /// </summary>
    internal LevenshtomatonFactory() { }

    /// <summary>
    /// Gets the global singleton instance of the <see cref="LevenshtomatonFactory"/>.
    /// </summary>
    public static LevenshtomatonFactory Instance { get; } = new ();

    /// <summary>
    /// Constructs a <see cref="Levenshtomaton"/> that accepts strings within a specified
    /// edit distance of the provided reference string.
    /// </summary>
    /// <param name="s">
    /// The reference string that defines the center of the Levenshtein distance computation.
    /// The resulting automaton will accept strings whose distance to this string is less than
    /// or equal to <paramref name="maxEditDistance"/>.
    /// </param>
    /// <param name="maxEditDistance">
    /// The inclusive maximum number of edit operations (insertions, deletions, or substitutions)
    /// allowed between the reference string and a candidate string.
    /// Must be non-negative.
    /// </param>
    /// <param name="ignoreCase">
    /// When <c>true</c>, the automaton will perform case-insensitive matching using the
    /// invariant culture.
    /// </param>
    /// <param name="metric">
    /// The edit distance algorithm to use. The following are supported:
    /// <list type="bullet">
    ///   <item>
    ///     <term><see cref="LevenshtypoMetric.Levenshtein"/></term>
    ///     <description>Classic Levenshtein distance: insert, delete, substitute</description>
    ///   </item>
    ///   <item>
    ///     <term><see cref="LevenshtypoMetric.RestrictedEdit"/></term>
    ///     <description>Optimal String Alignment</description>
    ///   </item>
    /// </list>
    /// </param>
    /// <returns>
    /// A thread-safe, immutable <see cref="Levenshtomaton"/> instance representing a DFA
    /// capable of determining whether an input string is within the given edit distance
    /// of <paramref name="s"/>.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="maxEditDistance"/> is negative.
    /// </exception>
    public Levenshtomaton Construct(string s, int maxEditDistance, bool ignoreCase = false, LevenshtypoMetric metric = LevenshtypoMetric.Levenshtein)
    {
        if (maxEditDistance < 0)
        {
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

            case (3, LevenshtypoMetric.Levenshtein, _):
                _d3Levenshtein ??= ParameterizedLevenshtomaton.CreateTemplate(maxEditDistance, metric);
                return _d3Levenshtein.Instantiate(s, ignoreCase);

            case (3, LevenshtypoMetric.RestrictedEdit, _):
                _d3RestrictedEdit ??= ParameterizedLevenshtomaton.CreateTemplate(maxEditDistance, metric);
                return _d3RestrictedEdit.Instantiate(s, ignoreCase);

#if NET8_0_OR_GREATER
            case ( >= 4 and <= 30, LevenshtypoMetric.Levenshtein, false):
                return new BitwiseLevenshteinLevenshtomaton<CaseSensitive>(s, maxEditDistance);

            case ( >= 4 and <= 30, LevenshtypoMetric.Levenshtein, true):
                return new BitwiseLevenshteinLevenshtomaton<CaseInsensitive>(s, maxEditDistance);

            case ( >= 4 and <= 30, LevenshtypoMetric.RestrictedEdit, false):
                return new BitwiseRestrictedEditLevenshtomaton<CaseSensitive>(s, maxEditDistance);

            case ( >= 4 and <= 30, LevenshtypoMetric.RestrictedEdit, true):
                return new BitwiseRestrictedEditLevenshtomaton<CaseInsensitive>(s, maxEditDistance);
#endif

            default:
                if (!ignoreCase)
                {
                    return new FallbackLevenshtomaton<CaseSensitive>(s, metric, maxEditDistance);
                }
                else
                {
                    return new FallbackLevenshtomaton<CaseInsensitive>(s, metric, maxEditDistance);
                }
        }
    }

    private record struct TemplateKey(int MaxEditDistance, LevenshtypoMetric Metric);
}