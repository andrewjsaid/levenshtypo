using System;
using System.Linq;
using System.Text;

namespace Levenshtypo;

internal sealed class Distance0Levenshtomaton<TCaseSensitivity> : Levenshtomaton where TCaseSensitivity : struct, ICaseSensitivity<TCaseSensitivity>
{
    private string _s;
    private Rune[] _sRune;

    public Distance0Levenshtomaton(string s, LevenshtypoMetric metric) : base(s, 0)
    {
        _s = s;
        _sRune = s.EnumerateRunes().ToArray();
        Metric = metric;
    }

    public override bool IgnoreCase => typeof(TCaseSensitivity) == typeof(CaseInsensitive);

    public override LevenshtypoMetric Metric { get; }

    public override T Execute<T>(ILevenshtomatonExecutor<T> executor) => executor.ExecuteAutomaton(StartSpecialized());

    public override bool Matches(ReadOnlySpan<char> text)
    {
        if (IgnoreCase)
        {
            return text.Equals(_s.AsSpan(), StringComparison.OrdinalIgnoreCase);
        }
        else
        {
            return text.SequenceEqual(_s.AsSpan());
        }
    }

    private State StartSpecialized() => new State(_sRune, 0);

    public override LevenshtomatonExecutionState Start() => LevenshtomatonExecutionState.FromStruct(StartSpecialized());

    private readonly struct State : ILevenshtomatonExecutionState<State>
    {
        private readonly Rune[] _sRune;
        private readonly int _sIndex;

        internal State(Rune[] sRune, int sIndex)
        {
            _sRune = sRune;
            _sIndex = sIndex;
        }

        public bool MoveNext(Rune c, out State next)
        {
            var sRune = _sRune;

            if (_sIndex < sRune.Length)
            {
                var sNext = sRune[_sIndex];
                if (default(TCaseSensitivity).Equals(sNext, c))
                {
                    next = new State(sRune, _sIndex + 1);
                    return true;
                }
            }

            next = default;
            return false;
        }

        public bool IsFinal => _sIndex == _sRune.Length;
    }
}
