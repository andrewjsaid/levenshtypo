namespace Levenshtypo;


/// <summary>
/// Represents a handler capable of executing a <see cref="Levenshtomaton"/> against an external
/// data structure or search domain, producing a result of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">
/// The result type produced by the execution (e.g., a match list, a boolean match indicator,
/// a best-scoring entry, etc.).
/// </typeparam>
/// <remarks>
/// This interface is designed for performance and composability. By using a generic value-type
/// state (<typeparamref name="TState"/>), the execution avoids boxing and enables the JIT to fully
/// specialize the automaton traversal logic for each distinct automaton configuration.
///
/// <para>
/// The <see cref="Levenshtomaton"/> does not return its state directly because doing so would
/// require boxing the underlying struct. Instead, execution is delegated to an implementation
/// of this interface, which receives the initial state and drives the traversal.
/// </para>
/// </remarks>
public interface ILevenshtomatonExecutor<T>
{
    /// <summary>
    /// Executes the automaton using the specified initial state, typically produced by
    /// <see cref="Levenshtomaton.Start"/> or <see cref="Levenshtomaton.Execute{T}"/>.
    /// </summary>
    /// <typeparam name="TState">
    /// The concrete value type implementing <see cref="ILevenshtomatonExecutionState{TState}"/>.
    /// This type encapsulates the current automaton transition state and distance tracking.
    /// </typeparam>
    /// <param name="executionState">
    /// The initial automaton execution state. The executor should use this value to begin traversal
    /// of its underlying data structure (e.g., a trie, token stream, or other searchable corpus).
    /// </param>
    /// <returns>
    /// The result of executing the automaton, defined by the semantics of the implementing type.
    /// For example, this may be a boolean match, a ranked list of results, or a filtered collection.
    /// </returns>
    T ExecuteAutomaton<TState>(TState executionState) where TState : struct, ILevenshtomatonExecutionState<TState>;
}
