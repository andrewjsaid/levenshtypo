namespace Levenshtypo;

/// <summary>
/// Represents a deterministic finite automaton (DFA) specialized for determining whether a given input
/// string is within a specified edit distance of a reference string using a configurable edit distance metric.
///
/// <para>
/// A <see cref="Levenshtomaton"/> is created via <see cref="LevenshtomatonFactory"/>, and it encapsulates
/// the transition table and final states required to evaluate insertions, deletions, and substitutions.
/// </para>
///
/// <para>
/// Instances of this class are immutable and thread-safe.
/// </para>
/// </summary>
public abstract class Levenshtomaton
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Levenshtomaton"/> class.
    /// This constructor is intended for use by subclasses.
    /// </summary>
    /// <param name="text">The reference string against which matches will be computed.</param>
    /// <param name="maxEditDistance">The maximum edit distance allowed for a successful match.</param>
    private protected Levenshtomaton(string text, int maxEditDistance)
    {
        Text = text;
        MaxEditDistance = maxEditDistance;
    }

    /// <summary>
    /// Gets the reference string against which candidate strings will be compared.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Gets the inclusive maximum number of edit operations allowed for a candidate string to be considered a match.
    /// </summary>
    public int MaxEditDistance { get; }

    /// <summary>
    /// Gets a value indicating whether this automaton performs case-insensitive matching.
    /// </summary>
    public abstract bool IgnoreCase { get; }

    /// <summary>
    /// Gets the edit distance metric used by this automaton.
    /// Supported values include Levenshtein and Restricted Edit Distance.
    /// </summary>
    public abstract LevenshtypoMetric Metric { get; }

    /// <summary>
    /// Begins optimized execution of the automaton using a provided executor strategy.
    /// This method avoids boxing and allocations and is suitable for high-performance scenarios.
    /// </summary>
    /// <typeparam name="TExecutor">The type of the module being executed.</typeparam>
    /// <typeparam name="TResult">The return type produced by the executor.</typeparam>
    /// <param name="executor">
    /// An object implementing <see cref="ILevenshtomatonExecutor{T}"/>, which handles traversal and result computation.
    /// </param>
    /// <returns>The result of executing the automaton against the input source.</returns>
    public abstract TResult Execute<TExecutor, TResult>(TExecutor executor) where TExecutor : ILevenshtomatonExecutor<TResult>
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
        ;

    /// <summary>
    /// Begins a general-purpose execution of the automaton.
    /// This method is simpler to use than <see cref="Execute{T}"/>, but may introduce
    /// additional allocations due to internal boxing of state.
    /// </summary>
    /// <returns>
    /// A <see cref="LevenshtomatonExecutionState"/> instance representing the initial state of execution.
    /// Callers can use this object to iterate through runes and evaluate matches.
    /// </returns>
    public abstract LevenshtomatonExecutionState Start();
}
