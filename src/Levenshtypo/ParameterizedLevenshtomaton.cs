using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Levenshtypo
{
    internal abstract class ParameterizedLevenshtomaton : Levenshtomaton
    {
        private protected ParameterizedLevenshtomaton(
            string text,
            int maxEditDistance) : base(text, maxEditDistance)
        {
        }

        protected static int CalculateMaxCharacterizedVectorLength(int maxEditDistance) => (2 * maxEditDistance) + 1;

        internal static Template CreateTemplate(int maxEditDistance)
        {
            var (nfaStates, nfaTransitions) = BuildNfa(maxEditDistance);
#if DEBUG
            ToDot(nfaStates, nfaTransitions);
#endif

            var (dfaStates, dfaTransitions) = ConvertToDfa(nfaStates, nfaTransitions, maxEditDistance);
#if DEBUG
            ToDot(dfaStates, dfaTransitions, maxEditDistance);
#endif

            return new Template(dfaStates, dfaTransitions, maxEditDistance);
        }

        private static (NfaState[] nfaStates, NfaTransition[] nfaTransitions) BuildNfa(int maxEditDistance)
        {
            int sLength = CalculateMaxCharacterizedVectorLength(maxEditDistance);
            var states = new NfaState[(sLength + 1) * (maxEditDistance + 1)];
            var transitions = new List<NfaTransition>(states.Length * 3);

            for (int i = 0; i <= sLength; i++)
            {
                for (int e = 0; e <= maxEditDistance; e++)
                {
                    var stateIndex = CalculateStateIndex(maxEditDistance, i, e);

                    var transitionStartIndex = transitions.Count;

                    ref var state = ref states[stateIndex];
                    state.Consumed = i;
                    state.Error = e;

                    if (i < sLength)
                    {
                        transitions.Add(new NfaTransition
                        {
                            MatchingCharacterOffset = i,
                            MatchingStateIndex = CalculateStateIndex(maxEditDistance, i + 1, e)
                        });

                        if (e < maxEditDistance)
                        {
                            // Add a character
                            transitions.Add(new NfaTransition
                            {
                                MatchingCharacterOffset = NfaTransition.MatchesAnyChar,
                                MatchingStateIndex = CalculateStateIndex(maxEditDistance, i, e + 1)
                            });

                            // Substitute a character
                            transitions.Add(new NfaTransition
                            {
                                MatchingCharacterOffset = NfaTransition.MatchesAnyChar,
                                MatchingStateIndex = CalculateStateIndex(maxEditDistance, i + 1, e + 1)
                            });
                        }

                        for (int iDelete = 1; iDelete <= maxEditDistance; iDelete++)
                        {
                            if (e + iDelete <= maxEditDistance && (i + iDelete) < sLength)
                            {
                                // Delete {iDelete} characters
                                transitions.Add(new NfaTransition
                                {
                                    MatchingCharacterOffset = i + iDelete,
                                    MatchingStateIndex = CalculateStateIndex(maxEditDistance, i + 1 + iDelete, e + iDelete)
                                });
                            }
                        }
                    }
                    else
                    {
                        if (e < maxEditDistance)
                        {
                            // Add a character
                            transitions.Add(new NfaTransition
                            {
                                MatchingCharacterOffset = NfaTransition.MatchesAnyChar,
                                MatchingStateIndex = CalculateStateIndex(maxEditDistance, i, e + 1)
                            });
                        }
                    }

                    state.TransitionStartIndex = transitionStartIndex;
                    state.TransitionCount = transitions.Count - transitionStartIndex;
                }
            }

            return (states, transitions.ToArray());
        }

        private static (DfaState[] dfaStates, DfaTransition[] dfaTransitions) ConvertToDfa(
            NfaState[] nfaStates,
            NfaTransition[] nfaTransitions,
            int maxEditDistance)
        {
            var maxCharacteristicVectorLength = CalculateMaxCharacterizedVectorLength(maxEditDistance);

            var states = new List<DfaState>();
            var transitions = new List<DfaTransition>();
            var multiStateBuilder = new List<int>();
            var transitionsBuilder = new List<char>();

            var seen = new List<int[]>();

            MapNfaStatesToDfaIdx(new int[] { 0 });

            return (states.ToArray(), transitions.ToArray());

            int MapNfaStatesToDfaIdx(int[] multiState)
            {
#if DEBUG
                var name = string.Join(" ", multiState.Select(ms => $"c{nfaStates[ms].Consumed}_e{nfaStates[ms].Error}"));
#endif

                // Check if it already exists
                for (int i = 0; i < seen.Count; i++)
                {
                    if (seen[i].SequenceEqual(multiState))
                    {
                        return i;
                    }
                }

                var dfaStartIdx = states.Count;

                var minCharacteristicVectorLength = 0;
                foreach (var stateIndex in multiState)
                {
                    // Basically this multiState can only be reached if we've consumed a certain number of characters,
                    // so calculating the transitions makes no sense here
                    minCharacteristicVectorLength = Math.Max(minCharacteristicVectorLength, nfaStates[stateIndex].Consumed);
                }

                // Add 1 state for each "distance"
                for (int characteristicVectorLength = maxCharacteristicVectorLength; characteristicVectorLength >= minCharacteristicVectorLength; characteristicVectorLength--)
                {
                    states.Add(new DfaState());
                    seen.Add(multiState);
                }

                // Add 1 state for each "distance"
                var dfaStateWriteIndex = dfaStartIdx;
                for (int characteristicVectorLength = maxCharacteristicVectorLength; characteristicVectorLength >= minCharacteristicVectorLength; characteristicVectorLength--)
                {
                    var transitionStartIndex = transitions.Count;
                    int transitionWriteIndex = transitionStartIndex;

                    var maxCharacteristicVectorValue = (1u << characteristicVectorLength) - 1;
                    for (uint characteristicVector = 0; characteristicVector <= maxCharacteristicVectorValue; characteristicVector++)
                    {
                        transitions.Add(new DfaTransition());
                    }

                    var baseVectorCharFlag = 1u << (characteristicVectorLength - 1);
                    for (uint characteristicVector = 0; characteristicVector <= maxCharacteristicVectorValue; characteristicVector++)
                    {
                        foreach (var nfaIndex in multiState)
                        {
                            var nfa = nfaStates[nfaIndex];

                            foreach (var transition in nfaTransitions.AsSpan(nfa.TransitionStartIndex, nfa.TransitionCount))
                            {
                                if (transition.MatchingCharacterOffset == NfaTransition.MatchesAnyChar || 0 != (characteristicVector & (baseVectorCharFlag >> transition.MatchingCharacterOffset)))
                                {
                                    multiStateBuilder.Add(transition.MatchingStateIndex);
                                }
                            }
                        }

                        int nextStateIndex = DfaTransition.NoMatchingState;
                        int indexOffset = 0;
                        if (multiStateBuilder.Count > 0)
                        {
                            int[] nextMultiState;
                            (nextMultiState, indexOffset) = CompleteMultiStates(characteristicVectorLength);
                            nextStateIndex = MapNfaStatesToDfaIdx(nextMultiState);

                            // Point directly to the state
                            nextStateIndex += maxCharacteristicVectorLength - characteristicVectorLength;
                        }

                        transitions[transitionWriteIndex++] = new DfaTransition
                        {
#if DEBUG
                            CharacterizedVector = characteristicVector,
#endif
                            IndexOffset = indexOffset,
                            MatchingStateStartIndex = nextStateIndex
                        };
                    }

                    states[dfaStateWriteIndex++] = new DfaState
                    {
#if DEBUG
                        Name = $"{name} [d={characteristicVectorLength}]",
#endif
                        GroupId = dfaStartIdx,
                        CharacteristicVectorLength = characteristicVectorLength,
                        TransitionStartIndex = transitionStartIndex,
                        IsFinal = AnyFinal(multiState, characteristicVectorLength)
                    };
                }

                return dfaStartIdx;
            }

            (int[] multiStates, int offset) CompleteMultiStates(int canConsume)
            {
                Debug.Assert(multiStateBuilder.Count > 0);

                int i = 0;
                while (i < multiStateBuilder.Count)
                {
                    bool truncated = nfaStates[multiStateBuilder[i]].Consumed > canConsume;
                    bool duplicate = false;

                    if (!truncated)
                    {
                        for (int j = 0; j < i; j++)
                        {
                            if (multiStateBuilder[i] == multiStateBuilder[j])
                            {
                                duplicate = true;
                            }
                        }
                    }

                    if (truncated || duplicate)
                    {
                        multiStateBuilder.RemoveAt(i);
                    }
                    else
                    {
                        i++;
                    }
                }

                i = 0;
                while (i < multiStateBuilder.Count)
                {
                    var isUseless = false;

                    var iNfa = nfaStates[multiStateBuilder[i]];
                    for (int j = 0; j < multiStateBuilder.Count; j++)
                    {
                        if (i != j)
                        {
                            var jNfa = nfaStates[multiStateBuilder[j]];
                            isUseless |= (iNfa.Error - jNfa.Error) >= Math.Abs(iNfa.Consumed - jNfa.Consumed);
                        }
                    }

                    if (isUseless)
                    {
                        multiStateBuilder.RemoveAt(i);
                    }
                    else
                    {
                        i++;
                    }
                }

                // Now we shift all states left to get minimum consumed to 0
                var minConsumed = int.MaxValue;
                foreach (var nfaIndex in multiStateBuilder)
                {
                    minConsumed = Math.Min(minConsumed, nfaStates[nfaIndex].Consumed);
                }

                if (minConsumed > 0)
                {
                    var replaceCount = multiStateBuilder.Count;
                    i = 0;
                    while (i < replaceCount)
                    {
                        var nfaState = nfaStates[multiStateBuilder[i++]];
                        multiStateBuilder.Add(CalculateStateIndex(maxEditDistance, nfaState.Consumed - minConsumed, nfaState.Error));
                    }
                    multiStateBuilder.RemoveRange(0, replaceCount);
                }

                multiStateBuilder.Sort();
                var result = multiStateBuilder.ToArray();
                multiStateBuilder.Clear();
                return (result, minConsumed);
            }

            bool AnyFinal(int[] multiState, int distanceToEnd)
            {
                foreach (var stateIndex in multiState)
                {
                    var nfaState = nfaStates[stateIndex];
                    if (nfaState.Consumed + (maxEditDistance - nfaState.Error) >= distanceToEnd)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        private static int CalculateStateIndex(int maxEditDistance, int consumed, int error) => (consumed * (maxEditDistance + 1)) + error;

#if DEBUG
        private static void ToDot(NfaState[] nfaStates, NfaTransition[] nfaTransitions)
        {
            var sb = new System.Text.StringBuilder();

            sb.AppendLine("digraph nfa {");

            for (int i = 0; i < nfaStates.Length; i++)
            {
                NfaState state = nfaStates[i];
                var name = $"c{state.Consumed}_e{state.Error}";

                sb.AppendLine($" \"{name}\";");

                var transitions = nfaTransitions.AsSpan(state.TransitionStartIndex, state.TransitionCount);
                foreach (var transition in transitions)
                {
                    var nextState = nfaStates[transition.MatchingStateIndex];
                    var nextStateName = $"c{nextState.Consumed}_e{nextState.Error}";
                    var symbol = transition.MatchingCharacterOffset == NfaTransition.MatchesAnyChar ? "*" : transition.MatchingCharacterOffset.ToString();
                    sb.AppendLine($"  \"{name}\" -> \"{nextStateName}\" [label=\"CharIndex: {symbol}\"];");
                }
            }

            sb.AppendLine("}");

            var dot = sb.ToString();
        }

        private static void ToDot(DfaState[] nfaStates, DfaTransition[] nfaTransitions, int maxEditDistance)
        {
            var maxCharacteristicVectorLength = CalculateMaxCharacterizedVectorLength(maxEditDistance);

            var sb = new System.Text.StringBuilder();

            sb.AppendLine("digraph nfa {");

            for (int i = 0; i < nfaStates.Length; i++)
            {
                DfaState state = nfaStates[i];
                var color = state.IsFinal ? "blue" : (i < maxCharacteristicVectorLength) ? "green" : "black";
                sb.AppendLine($" \"{state.Name}\" [color = {color}];");

                var numCharacteristicVectors = checked((int)(1u << state.CharacteristicVectorLength));
                var transitions = nfaTransitions.AsSpan(state.TransitionStartIndex, numCharacteristicVectors);

                var printTransitionsMap = new Dictionary<int, List<string>>();

                for (int transitionIndex = 0; transitionIndex < transitions.Length; transitionIndex++)
                {
                    var transition = transitions[transitionIndex];
                    var symbol = Convert.ToString(transition.CharacterizedVector, 2).PadLeft(state.CharacteristicVectorLength, '0'); // binary

                    if (transition.MatchingStateStartIndex != DfaTransition.NoMatchingState)
                    {
                        if (!printTransitionsMap.TryGetValue(transition.MatchingStateStartIndex, out var printTransitions))
                        {
                            printTransitionsMap.Add(transition.MatchingStateStartIndex, printTransitions = new List<string>());
                        }

                        printTransitions.Add($"{symbol}+{transition.IndexOffset}");

                        /*
                         * This connection exists but messes up the graph
                        if (transitionIndex == 0)
                        {
                            if (!printTransitionsMap.TryGetValue(transition.MatchingStateStartIndex + 1, out printTransitions))
                            {
                                printTransitionsMap.Add(transition.MatchingStateStartIndex + 1, printTransitions = new());
                            }

                            printTransitions.Add($"{symbol}+{transition.IndexOffset}_alt");
                        }
                        */
                    }
                }

                foreach (var (nextStateIndex, labels) in printTransitionsMap)
                {
                    var nextState = nfaStates[nextStateIndex];
                    var label = string.Join(" or ", labels);
                    sb.AppendLine($"  \"{state.Name}\" -> \"{nextState.Name}\" [label=\"{label}\"];");
                }
            }

            sb.AppendLine("}");

            var dot = sb.ToString();
        }
#endif

        internal sealed class Template
        {

            private readonly DfaState[] _states;
            private readonly DfaTransition[] _transitions;
            private readonly int _maxEditDistance;

            public Template(
                DfaState[] states,
                DfaTransition[] transitions,
                int maxEditDistance)
            {
                _states = states;
                _transitions = transitions;
                _maxEditDistance = maxEditDistance;
            }

            public ParameterizedLevenshtomaton Instantiate(string s, bool ignoreCase)
            {
                if (ignoreCase)
                {
                    return new ParameterizedLevenshtomaton<CaseInsensitive>(
                        _states,
                        _transitions,
                        s,
                        _maxEditDistance,
                        CalculateStartStateIndex(s.Length));
                }
                else
                {
                    return new ParameterizedLevenshtomaton<CaseSensitive>(
                        _states,
                        _transitions,
                        s,
                        _maxEditDistance,
                        CalculateStartStateIndex(s.Length));
                }
            }

            private int CalculateStartStateIndex(int sLength)
            {
                var characteristicVectorLength = CalculateMaxCharacterizedVectorLength(_maxEditDistance);
                var truncatedCharacteristicVectorLength = Math.Min(characteristicVectorLength, sLength);
                for (int i = 0; i < characteristicVectorLength; i++)
                {
                    if (_states[i].CharacteristicVectorLength == truncatedCharacteristicVectorLength)
                    {
                        return i;
                    }
                }
                throw new InvalidOperationException("Unable to find a starting state.");
            }
        }

        [DebuggerDisplay("{Consumed}[{Error}]")]
        internal struct NfaState
        {
            public int Consumed;
            public int Error;
            public int TransitionStartIndex;
            public int TransitionCount;
        }

        [DebuggerDisplay("MatchingChar: {MatchingChar}, Index: {MatchingStateIndex}")]
        internal struct NfaTransition
        {
            public const int MatchesAnyChar = -1;

            public int MatchingCharacterOffset;
            public int MatchingStateIndex;
        }

        [DebuggerDisplay("{Name}")]
        internal struct DfaState
        {
#if DEBUG
            public string Name;
#endif
            public int GroupId; // Used to avoid infinite loop
            public int CharacteristicVectorLength;
            public bool IsFinal;
            public int TransitionStartIndex;
        }

        [DebuggerDisplay("MatchingChar: {CharacterizedVector}, Index: {MatchingStateStartIndex}")]
        internal struct DfaTransition
        {
            public const int NoMatchingState = -1;

#if DEBUG
            public uint CharacterizedVector;
#endif
            public int MatchingStateStartIndex;
            public int IndexOffset;
        }
    }

    internal sealed class ParameterizedLevenshtomaton<TCaseSensitivity> : ParameterizedLevenshtomaton where TCaseSensitivity : struct, ICaseSensitivity<TCaseSensitivity>
    {
        private readonly DfaState[] _states;
        private readonly DfaTransition[] _transitions;
        private readonly string _s;
        private readonly int _maxEditDistance;
        private readonly int _startStateIndex;

        internal ParameterizedLevenshtomaton(
            DfaState[] states,
            DfaTransition[] transitions,
            string s,
            int maxEditDistance,
            int startStateIndex) : base(s, maxEditDistance)
        {
            _states = states;
            _transitions = transitions;
            _s = s;
            _maxEditDistance = maxEditDistance;
            _startStateIndex = startStateIndex;
        }

        public override bool Matches(ReadOnlySpan<char> text)
        {
            var state = Start();

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

        public override T Execute<T>(ILevenshtomatonExecutor<T> executor)
            => executor.ExecuteAutomaton(Start());

        private State Start() => new State(this, _startStateIndex, 0);

        internal override bool IgnoreCase => typeof(TCaseSensitivity) == typeof(CaseInsensitive);

        internal readonly struct State : ILevenshtomatonExecutionState<State>
        {
            private readonly ParameterizedLevenshtomaton<TCaseSensitivity> _automaton;
            private readonly int _stateIndex;
            private readonly int _sIndex;

            internal State(ParameterizedLevenshtomaton<TCaseSensitivity> automaton, int stateIndex, int sIndex)
            {
                _automaton = automaton;
                _stateIndex = stateIndex;
                _sIndex = sIndex;
            }

            public bool MoveNext(char c, out State next)
            {
                var automaton = _automaton;
                Debug.Assert(_automaton != null);

                var s = automaton._s;
                var sIndex = _sIndex;
                var states = _automaton._states;
                var maxEditDistance = automaton._maxEditDistance;

                var maxCharacteristicVectorLength = CalculateMaxCharacterizedVectorLength(maxEditDistance);
                var characteristicVectorLength = Math.Min(maxCharacteristicVectorLength, s.Length - sIndex);

                var characteristicVector = 0u;
                foreach (var sChar in s.AsSpan().Slice(sIndex, characteristicVectorLength))
                {
                    characteristicVector = (characteristicVector << 1) | (default(TCaseSensitivity).Equals(sChar, c) ? 1u : 0u);
                }

                var state = states[_stateIndex];
                var transition = automaton._transitions[state.TransitionStartIndex + characteristicVector];
                var nextCharacteristicVectorLength = Math.Min(maxCharacteristicVectorLength, s.Length - sIndex - transition.IndexOffset);

                if (transition.MatchingStateStartIndex != DfaTransition.NoMatchingState)
                {
                    int nextStateIndex = transition.MatchingStateStartIndex;
                    int groupId = states[transition.MatchingStateStartIndex].GroupId;

                    while (
                        states[nextStateIndex].CharacteristicVectorLength != nextCharacteristicVectorLength
                        && states[nextStateIndex].GroupId == groupId)
                    {
                        nextStateIndex++;
                    }

                    next = new State(automaton, nextStateIndex, _sIndex + transition.IndexOffset);
                    return true;
                }

                next = default;
                return false;
            }

            public bool IsFinal => _automaton._states[_stateIndex].IsFinal;
        }
    }
}
