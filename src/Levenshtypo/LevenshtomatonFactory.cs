using System.Collections.Concurrent;
using System.Diagnostics;

namespace Levenshtypo;

public sealed class LevenshtomatonFactory
{
    public const int MaxEditDistance = 7;

    private readonly ConcurrentDictionary<int, Template> _templates = new();

    public Levenshtomaton Construct(string s, int maxEditDistance)
    {
        if (maxEditDistance > MaxEditDistance)
        {
            // The limitation is purely for practical purposes there's no reason the implementation can't scale.
            // To support more than 7, we must increase size of characteristicVector from uint to whatever
            throw new ArgumentOutOfRangeException(nameof(maxEditDistance));
        }
        return _templates.GetOrAdd(maxEditDistance, CreateTemplate).Instantiate(s);
    }

    private static Template CreateTemplate(int maxEditDistance)
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
        int sLength = Levenshtomaton.CalculateMaxCharacterizedVectorLength(maxEditDistance);
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
        var maxCharacteristicVectorLength = Levenshtomaton.CalculateMaxCharacterizedVectorLength(maxEditDistance);

        var states = new List<DfaState>();
        var transitions = new List<DfaTransition>();
        var multiStateBuilder = new List<int>();
        var transitionsBuilder = new List<char>();

        var seen = new List<int[]>();

        MapNfaStatesToDfaIdx([0]);

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
                minCharacteristicVectorLength = int.Max(minCharacteristicVectorLength, nfaStates[stateIndex].Consumed);
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
                        (var nextMultiState, indexOffset) = CompleteMultiStates(characteristicVectorLength);
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

    static int CalculateStateIndex(int maxEditDistance, int consumed, int error) => (consumed * (maxEditDistance + 1)) + error;

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
        var maxCharacteristicVectorLength = Levenshtomaton.CalculateMaxCharacterizedVectorLength(maxEditDistance);

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
                        printTransitionsMap.Add(transition.MatchingStateStartIndex, printTransitions = new());
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

    private sealed class Template(
            DfaState[] states,
            DfaTransition[] transitions,
            int maxEditDistance)
    {

        private readonly DfaState[] _states = states;
        private readonly DfaTransition[] _transitions = transitions;
        private readonly int _maxEditDistance = maxEditDistance;

        public Levenshtomaton Instantiate(string s)
        {
            return new Levenshtomaton(
                _states,
                _transitions,
                s,
                _maxEditDistance,
                CalculateStartStateIndex(s.Length));
        }

        private int CalculateStartStateIndex(int sLength)
        {
            var characteristicVectorLength = Levenshtomaton.CalculateMaxCharacterizedVectorLength(_maxEditDistance);
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
}