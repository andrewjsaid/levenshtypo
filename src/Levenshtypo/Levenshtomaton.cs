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

        /// <summary>
        /// When true the the automaton is case insensitive.
        /// </summary>
        public abstract bool IgnoreCase { get; }

        /// <summary>
        /// The string edit distance metric used by the automaton.
        /// </summary>
        public abstract LevenshtypoMetric Metric { get; }

        /// <summary>
        /// Tests if a string matches <see cref="Text"/> within
        /// <see cref="MaxEditDistance"/> edits (insertions, deletions, substitutions).
        /// </summary>
        public abstract bool Matches(ReadOnlySpan<char> text);

        /// <summary>
        /// Begins execution of the automaton against a data structure.
        /// The data structure must be specialized to handle Levenshtomata
        /// and in doing so will be faster than <see cref="Start"/>.
        /// </summary>
        public abstract T Execute<T>(ILevenshtomatonExecutor<T> executor);

        /// <summary>
        /// Begins execution of the automaton. It is easier to get started
        /// using this method than using <see cref="Execute"/>, however
        /// this method introduces boxing during the execution.
        /// </summary>
        public abstract LevenshtomatonExecutionState Start();

        private protected bool DefaultMatchesImplementation<TState>(ReadOnlySpan<char> text, TState state) where TState : struct, ILevenshtomatonExecutionState<TState>
        {
            var i = 0;
            while (i < text.Length)
            {
                if (!state.MoveNext(text[i++], out state))
                {
                    return false;
                }
            }

            return state.IsFinal;
        }
    }

}
