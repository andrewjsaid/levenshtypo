﻿using System;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Levenshtypo;

// The main `State` class was generated by Levenshtypo.Generator

internal class Distance1LevenshteinLevenshtomaton<TCaseSensitivity> : Levenshtomaton where TCaseSensitivity : struct, ICaseSensitivity<TCaseSensitivity>
{
    private string _s;
    private Rune[] _sRune;

    public Distance1LevenshteinLevenshtomaton(string s) : base(s, 1)
    {
        _s = s;
        _sRune = s.EnumerateRunes().ToArray();
    }

    public override bool IgnoreCase => typeof(TCaseSensitivity) == typeof(CaseInsensitive);

    public override LevenshtypoMetric Metric => LevenshtypoMetric.Levenshtein;

    public override T Execute<T>(ILevenshtomatonExecutor<T> executor) => executor.ExecuteAutomaton(StartSpecialized());

    public override bool Matches(ReadOnlySpan<char> text, out int distance) => DefaultMatchesImplementation(text, StartSpecialized(), out distance);

    private State StartSpecialized() => State.Start(_sRune);

    public override LevenshtomatonExecutionState Start() => LevenshtomatonExecutionState.FromStruct(StartSpecialized());

    private readonly struct State : ILevenshtomatonExecutionState<State>
    {
        private static ReadOnlySpan<short> TransitionsData => [0x02, -1, -1, -1, -1, 0x01, 0x100, -1, 0x102, -1, 0x102, -1, -1, -1, -1, 0x01, 0x03, 0x100, 0x100, -1, 0x202, 0x102, 0x101, -1, -1, 0x102, 0x102, -1, 0x202, 0x102, 0x101, -1, -1, 0x102, 0x102, 0x01, 0x01, 0x03, 0x03, 0x100, 0x100, 0x100, 0x100, -1, -1, 0x202, 0x202, 0x102, 0x102, 0x101, 0x101, -1, -1, -1, -1, 0x102, 0x102, 0x102, 0x102, -1, 0x302, 0x202, 0x201, 0x102, 0x104, 0x101, 0x103, -1, 0x302, -1, 0x302, 0x102, 0x104, 0x102, 0x104];
        private static ReadOnlySpan<byte> DistanceData => [0x00, 0xFF, 0x01, 0xFF, 0xFF, 0x01, 0x01, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x01, 0x01, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF];

        private readonly Rune[] _sRune;
        private readonly int _sIndex;
        private readonly int _state;

        private State(Rune[] sRune, int state, int sIndex)
        {
            _sRune = sRune;
            _state = state;
            _sIndex = sIndex;
        }

        internal static State Start(Rune[] sRune) => new State(sRune, 0, 0);

        public bool MoveNext(Rune c, out State next)
        {
            var sRune = _sRune;
            var sIndex = _sIndex;

            var vectorLength = Math.Min(3, sRune.Length - sIndex);

            var vector = 0;
            foreach (var sChar in sRune.AsSpan(sIndex, vectorLength))
            {
                vector <<= 1;
                if (default(TCaseSensitivity).Equals(sChar, c))
                {
                    vector |= 1;
                }
            }

            var dStart = 5 * ((1 << vectorLength) - 1);
            var dOffset = _state * (1 << vectorLength) + vector;

            var encodedNext = TransitionsData[dStart + dOffset];

            if (encodedNext >= 0)
            {
                // format:
                // top bit reserved for negative sign
                // next 7 bits reserved for offset (max = 64 is more than enough)
                // next 8 bits is the nextState
                int nextState = encodedNext & 0xFF;
                int offset = (encodedNext >> 8) & 0x3F;
                next = new State(_sRune, nextState, sIndex + offset);
                return true;
            }

            next = default;
            return false;
        }

        public bool IsFinal =>
            0 != ((1ul << _state) & (_sRune.Length - _sIndex) switch
            {
                0 => 0x05ul,
                1 => 0x03ul,
                2 => 0x18ul,
                _ => 0x00ul,
            });

        public int Distance => DistanceData[Math.Min(3, _sRune.Length - _sIndex) * 5 + _state];
    }

}
