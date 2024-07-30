using System;

namespace Levenshtypo
{
    /// <summary>
    /// Represents an automaton (State Machine) which can determine whether a given string
    /// is within <see cref="MaxEditDistance"/> edits (insertions, deletions, substitutions)
    /// of the string <see cref="Text"/>
    /// </summary>
    public abstract class Levenshtomaton
    {
        private protected Levenshtomaton(string text, int maxEditDistance)
        {
            Text = text;
            MaxEditDistance = maxEditDistance;
        }

        /// <summary>
        /// The automaton is specialized to match words against this string.
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// The maximum edit distance (inclusive) for a positive match.
        /// </summary>
        public int MaxEditDistance { get; }

        internal abstract bool IgnoreCase { get; }

        /// <summary>
        /// Tests if a string matches <see cref="Text"/> within
        /// <see cref="MaxEditDistance"/> edits (insertions, deletions, substitutions).
        /// </summary>
        public abstract bool Matches(ReadOnlySpan<char> text);

        /// <summary>
        /// Execute the automaton against any data structure.
        /// </summary>
        public abstract T Execute<T>(ILevenshtomatonExecutor<T> executor);
    }

}
