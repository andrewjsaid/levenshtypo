using System;
using System.Linq;
using System.Text;

namespace Levenshtypo;

/// <summary>
/// A fallback state machine which works for any edit distance
/// but comes at a cost of performance.
/// </summary>
internal class FallbackLevenshtomaton<TCaseSensitivity> : Levenshtomaton where TCaseSensitivity : struct, ICaseSensitivity<TCaseSensitivity>
{
    private readonly Rune[] _sRune;

    public FallbackLevenshtomaton(
        string text,
        LevenshtypoMetric metric,
        int maxEditDistance) : base (text, maxEditDistance)
    {
        _sRune = text.EnumerateRunes().ToArray();
        Metric = metric;
    }

    public override bool IgnoreCase => typeof(TCaseSensitivity) == typeof(CaseInsensitive);

    public override LevenshtypoMetric Metric { get; }

    public override T Execute<T>(ILevenshtomatonExecutor<T> executor)
    {
        return Metric switch
        {
            LevenshtypoMetric.Levenshtein => executor.ExecuteAutomaton(LevenshteinState.Start(_sRune, maxEditDistance: MaxEditDistance)),
            LevenshtypoMetric.RestrictedEdit => executor.ExecuteAutomaton(RestrictedEditState.Start(_sRune, maxEditDistance: MaxEditDistance)),
            _ => throw new NotSupportedException()
        };
    }

    public override bool Matches(ReadOnlySpan<char> text, out int distance)
    {
        return Metric switch
        {
            LevenshtypoMetric.Levenshtein => DefaultMatchesImplementation(text, LevenshteinState.Start(_sRune, maxEditDistance: MaxEditDistance), out distance),
            LevenshtypoMetric.RestrictedEdit => DefaultMatchesImplementation(text, RestrictedEditState.Start(_sRune, maxEditDistance: MaxEditDistance), out distance),
            _ => throw new NotSupportedException()
        };
    }

    public override LevenshtomatonExecutionState Start()
    {
        return Metric switch
        {
            LevenshtypoMetric.Levenshtein => LevenshtomatonExecutionState.FromStruct(LevenshteinState.Start(_sRune, maxEditDistance: MaxEditDistance)),
            LevenshtypoMetric.RestrictedEdit => LevenshtomatonExecutionState.FromStruct(RestrictedEditState.Start(_sRune, maxEditDistance: MaxEditDistance)),
            _ => throw new NotSupportedException()
        };
    }

    private readonly struct LevenshteinState : ILevenshtomatonExecutionState<LevenshteinState>
    {
        private readonly Rune[] _sRunes;
        private readonly int _cIndex;
        private readonly int _maxEditDistance;
        private readonly int[] _state;

        private LevenshteinState(Rune[] sRunes, int cIndex, int maxEditDistance, int[] state)
        {
            _sRunes = sRunes;
            _cIndex = cIndex;
            _maxEditDistance = maxEditDistance;
            _state = state;
        }

        public static LevenshteinState Start(Rune[] sRunes, int maxEditDistance)
        {
            var state = new int[sRunes.Length + 1];
            for (int i = 0; i < state.Length; i++)
            {
                state[i] = i;
            }

            return new LevenshteinState(sRunes, 0, maxEditDistance, state);
        }

        public bool MoveNext(Rune c, out LevenshteinState next)
        {
            // Copy any changes to the other State too

            var sRunes = _sRunes;
            var state = _state;
            var nextState = new int[state.Length];
            nextState[0] = _cIndex + 1;

            var globalMin = nextState[0];

            for (int i = 0; i < sRunes.Length; i++)
            {
                var cost = default(TCaseSensitivity).Equals(sRunes[i], c) ? 0 : 1;
                var deletionCost = state[i + 1] + 1;
                var insertionCost = nextState[i] + 1;
                var substitutionCost = state[i] + cost;
                var min = Math.Min(Math.Min(deletionCost, insertionCost), substitutionCost);

                nextState[i + 1] = min;
                globalMin = Math.Min(min, globalMin);
            }

            if (globalMin <= _maxEditDistance)
            {
                next = new LevenshteinState(sRunes, _cIndex + 1, _maxEditDistance, nextState);
                return true;
            }

            next = default!;
            return false;
        }

        public bool IsFinal => _state[^1] <= _maxEditDistance;

        public int Distance => _state[^1];
    }

    private readonly struct RestrictedEditState : ILevenshtomatonExecutionState<RestrictedEditState>
    {
        private readonly Rune[] _sRunes;
        private readonly int _cIndex;
        private readonly int _maxEditDistance;
        private readonly int[] _state;
        private readonly int[]? _prevState;
        private readonly Rune _prevChar;

        private RestrictedEditState(
            Rune[] sRunes, 
            int cIndex, 
            int maxEditDistance, 
            int[] state, 
            int[]? prevState,
            Rune prevChar)
        {
            _sRunes = sRunes;
            _cIndex = cIndex;
            _maxEditDistance = maxEditDistance;
            _state = state;
            _prevState = prevState;
            _prevChar = prevChar;
        }

        public static RestrictedEditState Start(Rune[] sRunes, int maxEditDistance)
        {
            var state = new int[sRunes.Length + 1];
            for (int i = 0; i < state.Length; i++)
            {
                state[i] = i;
            }

            return new RestrictedEditState(sRunes, 0, maxEditDistance, state, null, default);
        }

        public bool MoveNext(Rune c, out RestrictedEditState next)
        {
            // Copy any changes to the other State too

            var sRunes = _sRunes;
            var state = _state;
            var prevState = _prevState;
            var prevChar = _prevChar;

            var nextState = new int[state.Length];
            nextState[0] = _cIndex + 1;

            var globalMin = nextState[0];

            for (int i = 0; i < sRunes.Length; i++)
            {
                var cost = default(TCaseSensitivity).Equals(sRunes[i], c) ? 0 : 1;
                var deletionCost = state[i + 1] + 1;
                var insertionCost = nextState[i] + 1;
                var substitutionCost = state[i] + cost;
                var min = Math.Min(Math.Min(deletionCost, insertionCost), substitutionCost);

                if (prevState is not null
                    && i > 0
                    && default(TCaseSensitivity).Equals(sRunes[i - 1], c)
                    && default(TCaseSensitivity).Equals(sRunes[i - 0], prevChar))
                {
                    min = Math.Min(min, prevState[i - 1] + 1);
                }

                nextState[i + 1] = min;
                globalMin = Math.Min(min, globalMin);
            }

            if (globalMin <= _maxEditDistance)
            {
                next = new RestrictedEditState(
                    sRunes, 
                    _cIndex + 1, 
                    _maxEditDistance, 
                    nextState,
                    prevState: state,
                    prevChar: c);
                return true;
            }

            next = default!;
            return false;
        }

        public bool IsFinal => _state[^1] <= _maxEditDistance;

        public int Distance => _state[^1];
    }
}
