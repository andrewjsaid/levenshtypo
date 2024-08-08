using System.Text;

namespace Levenshtypo;

/// <summary>
/// Represents execution of an automaton.
/// </summary>
/// <remarks>
/// The generics here is to balance power and performance.
/// It allows for double dispatch to avoid boxing.
/// In using this format of generics (struct), the users of this interface
/// will force the JIT to compile a specialized method for each type of struct.
/// </remarks>
public interface ILevenshtomatonExecutionState<TSelf> where TSelf : ILevenshtomatonExecutionState<TSelf>
{
    /// <summary>
    /// Consume a character in the input string, advancing the automaton to the next state.
    /// </summary>
    /// <param name="c">The character being consumed.</param>
    /// <param name="next">The next state of the automaton, after the character has been consumed.</param>
    /// <returns>Whether a next state was found.</returns>
    bool MoveNext(Rune c, out TSelf next);

    /// <summary>
    /// When true, the characters leading up to this state form text
    /// which is within the Levenshtein Distance of the original
    /// text.
    /// </summary>
    bool IsFinal { get; }
}

/// <summary>
/// Represents execution of an automaton.
/// </summary>
/// <remarks>
/// This version of the automaton execution state relies on boxing and
/// should be avoided in performance critical scenarios.
/// </remarks>
public abstract class LevenshtomatonExecutionState : ILevenshtomatonExecutionState<LevenshtomatonExecutionState>
{
    /// <summary>
    /// Consume a character in the input string, advancing the automaton to the next state.
    /// </summary>
    /// <param name="c">The character being consumed.</param>
    /// <param name="next">The next state of the automaton, after the character has been consumed.</param>
    /// <returns>Whether a next state was found.</returns>
    public abstract bool MoveNext(Rune c, out LevenshtomatonExecutionState next);

    /// <summary>
    /// When true, the characters leading up to this state form text
    /// which is within the Edit Distance of the original text.
    /// </summary>
    public abstract bool IsFinal { get; }

    /// <summary>
    /// Wraps a struct implementation of <see cref="ILevenshtomatonExecutionState{TSelf}"/> into a class.
    /// </summary>
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
}
