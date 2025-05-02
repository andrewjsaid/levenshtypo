using System.Text;

namespace Levenshtypo;

/// <summary>
/// Defines a value-type representation of an automaton execution state that can be advanced
/// over Unicode scalar values (<see cref="Rune"/>).
/// </summary>
/// <typeparam name="TSelf">
/// The implementing type itself. This is a self-referential generic constraint used to enable
/// efficient, allocation-free execution and avoid boxing during state transitions.
/// </typeparam>
/// <remarks>
/// This interface is designed for high-performance execution of Levenshtomata. By requiring
/// that <typeparamref name="TSelf"/> be a <c>struct</c>, the JIT can specialize and inline
/// automaton logic, avoiding interface dispatch and heap allocations.
///
/// <para>
/// This design pattern enables double dispatch over `struct`-based states, allowing efficient
/// traversal without sacrificing polymorphism.
/// </para>
/// </remarks>
public interface ILevenshtomatonExecutionState<TSelf> where TSelf : ILevenshtomatonExecutionState<TSelf>
{
    /// <summary>
    /// Advances the automaton to its next state by consuming a Unicode scalar value.
    /// </summary>
    /// <param name="c">The Unicode scalar value (<see cref="Rune"/>) to consume.</param>
    /// <param name="next">
    /// When this method returns <c>true</c>, contains the resulting state after the transition.
    /// When it returns <c>false</c>, the transition was invalid and no next state exists.
    /// </param>
    /// <returns>
    /// <c>true</c> if a valid state transition was found for <paramref name="c"/>; otherwise, <c>false</c>.
    /// </returns>
    bool MoveNext(Rune c, out TSelf next);

    /// <summary>
    /// Gets a value indicating whether the current state is a final (accepting) state.
    /// </summary>
    /// <remarks>
    /// A final state indicates that the characters consumed so far form a string that is
    /// within the configured edit distance of the reference string.
    /// </remarks>
    bool IsFinal { get; }

    /// <summary>
    /// Gets the edit distance between the consumed input and the reference string
    /// when in a final (accepting) state.
    /// </summary>
    /// <remarks>
    /// The value is meaningful only when <see cref="IsFinal"/> is <c>true</c>.
    /// </remarks>
    int Distance { get; }
}

/// <summary>
/// Represents a boxed, reference-type version of an automaton execution state.
/// </summary>
/// <remarks>
/// This class enables execution of a Levenshtomaton using a polymorphic interface
/// without requiring knowledge of the underlying struct implementation.
///
/// <para>
/// While easier to work with in general-purpose APIs, this abstraction introduces
/// boxing and is not suitable for performance-critical or allocation-sensitive scenarios.
/// </para>
/// </remarks>
public abstract class LevenshtomatonExecutionState : ILevenshtomatonExecutionState<LevenshtomatonExecutionState>
{
    /// <inheritdoc />
    public abstract bool MoveNext(Rune c, out LevenshtomatonExecutionState next);

    /// <inheritdoc />
    public abstract bool IsFinal { get; }

    /// <inheritdoc />
    public abstract int Distance { get; }

    /// <summary>
    /// Wraps a value-type automaton state into a reference-type <see cref="LevenshtomatonExecutionState"/>.
    /// </summary>
    /// <typeparam name="TState">
    /// The struct type implementing <see cref="ILevenshtomatonExecutionState{TState}"/>.
    /// </typeparam>
    /// <param name="state">The value-type execution state to wrap.</param>
    /// <returns>
    /// A boxed <see cref="LevenshtomatonExecutionState"/> that delegates to the original struct implementation.
    /// </returns>
    public static LevenshtomatonExecutionState FromStruct<TState>(TState state) where TState : struct, ILevenshtomatonExecutionState<TState>
    {
        return new StructWrappedLevenshtomatonExecutionState<TState>(state);
    }
}

internal sealed class StructWrappedLevenshtomatonExecutionState<TState> : LevenshtomatonExecutionState where TState : struct, ILevenshtomatonExecutionState<TState>
{
    private TState _state;

    public StructWrappedLevenshtomatonExecutionState(TState state)
    {
        _state = state;
    }

    public override bool MoveNext(Rune c, out LevenshtomatonExecutionState next)
    {
        var result = _state.MoveNext(c, out var nextState);
        next = new StructWrappedLevenshtomatonExecutionState<TState>(nextState);
        return result;
    }

    public override bool IsFinal => _state.IsFinal;

    public override int Distance => _state.Distance;
}
