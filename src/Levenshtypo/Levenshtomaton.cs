using System;
using System.Diagnostics;

namespace Levenshtypo
{

    public sealed class Levenshtomaton
    {
        private readonly DfaState[] _states;
        private readonly DfaTransition[] _transitions;
        private readonly string _s;
        private readonly int _maxEditDistance;
        private readonly int _startStateIndex;

        internal Levenshtomaton(
            DfaState[] states,
            DfaTransition[] transitions,
            string s,
            int maxEditDistance,
            int startStateIndex)
        {
            _states = states;
            _transitions = transitions;
            _s = s;
            _maxEditDistance = maxEditDistance;
            _startStateIndex = startStateIndex;
        }

        public bool Matches(ReadOnlySpan<char> text)
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

        public State Start() => new State(this, _startStateIndex, 0);

        internal static int CalculateMaxCharacterizedVectorLength(int maxEditDistance) => (2 * maxEditDistance) + 1;

        public readonly struct State
        {
            private readonly Levenshtomaton _automaton;
            private readonly int _stateIndex;
            private readonly int _sIndex;

            internal State(Levenshtomaton automaton, int stateIndex, int sIndex)
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
                    characteristicVector = (characteristicVector << 1) | (sChar == c ? 1u : 0u);
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
