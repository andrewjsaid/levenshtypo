﻿using System;

namespace Levenshtypo;

internal class Distance0Levenshtomaton<TCaseSensitivity> : Levenshtomaton where TCaseSensitivity : struct, ICaseSensitivity<TCaseSensitivity>
{
    private string _s;

    public Distance0Levenshtomaton(string s, LevenshtypoMetric metric) : base(s, 0)
    {
        _s = s;
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

    private State StartSpecialized() => new State(_s, 0);

    public override LevenshtomatonExecutionState Start() => new LevenshtomatonExecutionState<State>(StartSpecialized());

    private readonly struct State : ILevenshtomatonExecutionState<State>
    {
        private readonly string _s;
        private readonly int _sIndex;

        internal State(string s, int sIndex)
        {
            _s = s;
            _sIndex = sIndex;
        }

        public bool MoveNext(char c, out State next)
        {
            if (_sIndex < _s.Length)
            {
                var sNext = _s[_sIndex];
                if (default(TCaseSensitivity).Equals(sNext, c))
                {
                    next = new State(_s, _sIndex + 1);
                    return true;
                }
            }

            next = default;
            return false;
        }

        public bool IsFinal => _sIndex == _s.Length;
    }
}
