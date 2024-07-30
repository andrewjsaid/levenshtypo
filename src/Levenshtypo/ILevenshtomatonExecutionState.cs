namespace Levenshtypo
{

    /// <summary>
    /// Represents execution of an automaton.
    /// </summary>
    /// <remarks>
    /// The generics here is to balance power and performance.
    /// In using this format of generics (struct), the users of this interface
    /// will force the JIT to compile a specialized method for each type of struct.
    /// </remarks>
    public interface ILevenshtomatonExecutionState<TSelf> where TSelf : struct, ILevenshtomatonExecutionState<TSelf>
    {
        /// <summary>
        /// Consume a character in the input string, advancing the automaton to the next state.
        /// </summary>
        /// <param name="c">The character being consumed.</param>
        /// <param name="next">The next state of the automaton, after the character has been consumed.</param>
        /// <returns>Whether a next state was found.</returns>
        bool MoveNext(char c, out TSelf next);

        /// <summary>
        /// When true, the characters leading up to this state form text
        /// which is within the Levenshtein Distance of the original
        /// text.
        /// </summary>
        bool IsFinal { get; }
    }

}
