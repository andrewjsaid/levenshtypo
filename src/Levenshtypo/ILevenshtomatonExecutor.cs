namespace Levenshtypo
{
    /// <summary>
    /// To run the automaton against a data structure it must implement this interface.
    /// </summary>
    /// <typeparam name="T">Represents the result type of the search algorithm.</typeparam>
    /// <remarks>
    /// The generics here is to balance power and performance.
    /// In using this format of generics (struct), the users of this interface
    /// will force the JIT to compile a specialized method for each type of struct.
    /// 
    /// Another quirk here is that we can't just return the <see cref="ILevenshtomatonExecutionState{TSelf}"/>
    /// from the automaton as that would cause boxing, thus we must instead accept an instance
    /// of this interface and use generics over <see cref="ExecuteAutomaton{TState}(TState)"/>
    /// to achieve good performance.
    /// </remarks>
    public interface ILevenshtomatonExecutor<T>
    {
        /// <summary>
        /// Execute the automaton against the data structure.
        /// </summary>
        /// <typeparam name="TState">The state of the automaton. The exact type is an implementation detail.</typeparam>
        /// <param name="executionState">Starts off representing the initial state of the automaton.</param>
        /// <returns>The results of the search algorithm, defined by the implementation.</returns>
        T ExecuteAutomaton<TState>(TState executionState) where TState : struct, ILevenshtomatonExecutionState<TState>;
    }
}
