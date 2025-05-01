#if NET8_0_OR_GREATER

using System;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Levenshtypo;

internal class BitwiseRestrictedEditLevenshtomaton<TCaseSensitivity> : Levenshtomaton where TCaseSensitivity : struct, ICaseSensitivity<TCaseSensitivity>
{
    private const int MaxSupportedEditDistance = 30;

    private readonly Rune[] _sRune;

    public BitwiseRestrictedEditLevenshtomaton(string s, int maxEditDistance) : base(s, maxEditDistance)
    {
        _sRune = s.EnumerateRunes().ToArray();

        if (maxEditDistance is <= 0 or > MaxSupportedEditDistance)
        {
            throw new NotSupportedException($"BitwiseLevenshteinLevenshtomaton supports up to distance {MaxSupportedEditDistance}.");
        }
    }

    public override bool IgnoreCase => typeof(TCaseSensitivity) == typeof(CaseInsensitive);

    public override LevenshtypoMetric Metric => LevenshtypoMetric.RestrictedEdit;

    public override T Execute<T>(ILevenshtomatonExecutor<T> executor)
    {
        return MaxEditDistance switch
        {
            01 => executor.ExecuteAutomaton(State<BitwiseBuffers.BufferDistance1, byte>.Start(_sRune)),
            02 => executor.ExecuteAutomaton(State<BitwiseBuffers.BufferDistance2, byte>.Start(_sRune)),
            03 => executor.ExecuteAutomaton(State<BitwiseBuffers.BufferDistance3, byte>.Start(_sRune)),
            04 => executor.ExecuteAutomaton(State<BitwiseBuffers.BufferDistance4, ushort>.Start(_sRune)),
            05 => executor.ExecuteAutomaton(State<BitwiseBuffers.BufferDistance5, ushort>.Start(_sRune)),
            06 => executor.ExecuteAutomaton(State<BitwiseBuffers.BufferDistance6, ushort>.Start(_sRune)),
            07 => executor.ExecuteAutomaton(State<BitwiseBuffers.BufferDistance7, ushort>.Start(_sRune)),
            08 => executor.ExecuteAutomaton(State<BitwiseBuffers.BufferDistance8, uint>.Start(_sRune)),
            09 => executor.ExecuteAutomaton(State<BitwiseBuffers.BufferDistance9, uint>.Start(_sRune)),
            10 => executor.ExecuteAutomaton(State<BitwiseBuffers.BufferDistance10, uint>.Start(_sRune)),
            11 => executor.ExecuteAutomaton(State<BitwiseBuffers.BufferDistance11, uint>.Start(_sRune)),
            12 => executor.ExecuteAutomaton(State<BitwiseBuffers.BufferDistance12, uint>.Start(_sRune)),
            13 => executor.ExecuteAutomaton(State<BitwiseBuffers.BufferDistance13, uint>.Start(_sRune)),
            14 => executor.ExecuteAutomaton(State<BitwiseBuffers.BufferDistance14, uint>.Start(_sRune)),
            15 => executor.ExecuteAutomaton(State<BitwiseBuffers.BufferDistance15, uint>.Start(_sRune)),
            16 => executor.ExecuteAutomaton(State<BitwiseBuffers.BufferDistance16, ulong>.Start(_sRune)),
            17 => executor.ExecuteAutomaton(State<BitwiseBuffers.BufferDistance17, ulong>.Start(_sRune)),
            18 => executor.ExecuteAutomaton(State<BitwiseBuffers.BufferDistance18, ulong>.Start(_sRune)),
            19 => executor.ExecuteAutomaton(State<BitwiseBuffers.BufferDistance19, ulong>.Start(_sRune)),
            20 => executor.ExecuteAutomaton(State<BitwiseBuffers.BufferDistance20, ulong>.Start(_sRune)),
            21 => executor.ExecuteAutomaton(State<BitwiseBuffers.BufferDistance21, ulong>.Start(_sRune)),
            22 => executor.ExecuteAutomaton(State<BitwiseBuffers.BufferDistance22, ulong>.Start(_sRune)),
            23 => executor.ExecuteAutomaton(State<BitwiseBuffers.BufferDistance23, ulong>.Start(_sRune)),
            24 => executor.ExecuteAutomaton(State<BitwiseBuffers.BufferDistance24, ulong>.Start(_sRune)),
            25 => executor.ExecuteAutomaton(State<BitwiseBuffers.BufferDistance25, ulong>.Start(_sRune)),
            26 => executor.ExecuteAutomaton(State<BitwiseBuffers.BufferDistance26, ulong>.Start(_sRune)),
            27 => executor.ExecuteAutomaton(State<BitwiseBuffers.BufferDistance27, ulong>.Start(_sRune)),
            28 => executor.ExecuteAutomaton(State<BitwiseBuffers.BufferDistance28, ulong>.Start(_sRune)),
            29 => executor.ExecuteAutomaton(State<BitwiseBuffers.BufferDistance29, ulong>.Start(_sRune)),
            30 => executor.ExecuteAutomaton(State<BitwiseBuffers.BufferDistance30, ulong>.Start(_sRune)),
            _ => throw new NotSupportedException()
        };
    }

    public override bool Matches(ReadOnlySpan<char> text, out int distance)
    {
        return MaxEditDistance switch
        {
            01 => DefaultMatchesImplementation(text, State<BitwiseBuffers.BufferDistance1, byte>.Start(_sRune), out distance),
            02 => DefaultMatchesImplementation(text, State<BitwiseBuffers.BufferDistance2, byte>.Start(_sRune), out distance),
            03 => DefaultMatchesImplementation(text, State<BitwiseBuffers.BufferDistance3, byte>.Start(_sRune), out distance),
            04 => DefaultMatchesImplementation(text, State<BitwiseBuffers.BufferDistance4, ushort>.Start(_sRune), out distance),
            05 => DefaultMatchesImplementation(text, State<BitwiseBuffers.BufferDistance5, ushort>.Start(_sRune), out distance),
            06 => DefaultMatchesImplementation(text, State<BitwiseBuffers.BufferDistance6, ushort>.Start(_sRune), out distance),
            07 => DefaultMatchesImplementation(text, State<BitwiseBuffers.BufferDistance7, ushort>.Start(_sRune), out distance),
            08 => DefaultMatchesImplementation(text, State<BitwiseBuffers.BufferDistance8, uint>.Start(_sRune), out distance),
            09 => DefaultMatchesImplementation(text, State<BitwiseBuffers.BufferDistance9, uint>.Start(_sRune), out distance),
            10 => DefaultMatchesImplementation(text, State<BitwiseBuffers.BufferDistance10, uint>.Start(_sRune), out distance),
            11 => DefaultMatchesImplementation(text, State<BitwiseBuffers.BufferDistance11, uint>.Start(_sRune), out distance),
            12 => DefaultMatchesImplementation(text, State<BitwiseBuffers.BufferDistance12, uint>.Start(_sRune), out distance),
            13 => DefaultMatchesImplementation(text, State<BitwiseBuffers.BufferDistance13, uint>.Start(_sRune), out distance),
            14 => DefaultMatchesImplementation(text, State<BitwiseBuffers.BufferDistance14, uint>.Start(_sRune), out distance),
            15 => DefaultMatchesImplementation(text, State<BitwiseBuffers.BufferDistance15, uint>.Start(_sRune), out distance),
            16 => DefaultMatchesImplementation(text, State<BitwiseBuffers.BufferDistance16, ulong>.Start(_sRune), out distance),
            17 => DefaultMatchesImplementation(text, State<BitwiseBuffers.BufferDistance17, ulong>.Start(_sRune), out distance),
            18 => DefaultMatchesImplementation(text, State<BitwiseBuffers.BufferDistance18, ulong>.Start(_sRune), out distance),
            19 => DefaultMatchesImplementation(text, State<BitwiseBuffers.BufferDistance19, ulong>.Start(_sRune), out distance),
            20 => DefaultMatchesImplementation(text, State<BitwiseBuffers.BufferDistance20, ulong>.Start(_sRune), out distance),
            21 => DefaultMatchesImplementation(text, State<BitwiseBuffers.BufferDistance21, ulong>.Start(_sRune), out distance),
            22 => DefaultMatchesImplementation(text, State<BitwiseBuffers.BufferDistance22, ulong>.Start(_sRune), out distance),
            23 => DefaultMatchesImplementation(text, State<BitwiseBuffers.BufferDistance23, ulong>.Start(_sRune), out distance),
            24 => DefaultMatchesImplementation(text, State<BitwiseBuffers.BufferDistance24, ulong>.Start(_sRune), out distance),
            25 => DefaultMatchesImplementation(text, State<BitwiseBuffers.BufferDistance25, ulong>.Start(_sRune), out distance),
            26 => DefaultMatchesImplementation(text, State<BitwiseBuffers.BufferDistance26, ulong>.Start(_sRune), out distance),
            27 => DefaultMatchesImplementation(text, State<BitwiseBuffers.BufferDistance27, ulong>.Start(_sRune), out distance),
            28 => DefaultMatchesImplementation(text, State<BitwiseBuffers.BufferDistance28, ulong>.Start(_sRune), out distance),
            29 => DefaultMatchesImplementation(text, State<BitwiseBuffers.BufferDistance29, ulong>.Start(_sRune), out distance),
            30 => DefaultMatchesImplementation(text, State<BitwiseBuffers.BufferDistance30, ulong>.Start(_sRune), out distance),
            _ => throw new NotSupportedException()
        };
    }

    public override LevenshtomatonExecutionState Start()
    {
        return MaxEditDistance switch
        {
            01 => LevenshtomatonExecutionState.FromStruct(State<BitwiseBuffers.BufferDistance1, byte>.Start(_sRune)),
            02 => LevenshtomatonExecutionState.FromStruct(State<BitwiseBuffers.BufferDistance2, byte>.Start(_sRune)),
            03 => LevenshtomatonExecutionState.FromStruct(State<BitwiseBuffers.BufferDistance3, byte>.Start(_sRune)),
            04 => LevenshtomatonExecutionState.FromStruct(State<BitwiseBuffers.BufferDistance4, ushort>.Start(_sRune)),
            05 => LevenshtomatonExecutionState.FromStruct(State<BitwiseBuffers.BufferDistance5, ushort>.Start(_sRune)),
            06 => LevenshtomatonExecutionState.FromStruct(State<BitwiseBuffers.BufferDistance6, ushort>.Start(_sRune)),
            07 => LevenshtomatonExecutionState.FromStruct(State<BitwiseBuffers.BufferDistance7, ushort>.Start(_sRune)),
            08 => LevenshtomatonExecutionState.FromStruct(State<BitwiseBuffers.BufferDistance8, uint>.Start(_sRune)),
            09 => LevenshtomatonExecutionState.FromStruct(State<BitwiseBuffers.BufferDistance9, uint>.Start(_sRune)),
            10 => LevenshtomatonExecutionState.FromStruct(State<BitwiseBuffers.BufferDistance10, uint>.Start(_sRune)),
            11 => LevenshtomatonExecutionState.FromStruct(State<BitwiseBuffers.BufferDistance11, uint>.Start(_sRune)),
            12 => LevenshtomatonExecutionState.FromStruct(State<BitwiseBuffers.BufferDistance12, uint>.Start(_sRune)),
            13 => LevenshtomatonExecutionState.FromStruct(State<BitwiseBuffers.BufferDistance13, uint>.Start(_sRune)),
            14 => LevenshtomatonExecutionState.FromStruct(State<BitwiseBuffers.BufferDistance14, uint>.Start(_sRune)),
            15 => LevenshtomatonExecutionState.FromStruct(State<BitwiseBuffers.BufferDistance15, uint>.Start(_sRune)),
            16 => LevenshtomatonExecutionState.FromStruct(State<BitwiseBuffers.BufferDistance16, ulong>.Start(_sRune)),
            17 => LevenshtomatonExecutionState.FromStruct(State<BitwiseBuffers.BufferDistance17, ulong>.Start(_sRune)),
            18 => LevenshtomatonExecutionState.FromStruct(State<BitwiseBuffers.BufferDistance18, ulong>.Start(_sRune)),
            19 => LevenshtomatonExecutionState.FromStruct(State<BitwiseBuffers.BufferDistance19, ulong>.Start(_sRune)),
            20 => LevenshtomatonExecutionState.FromStruct(State<BitwiseBuffers.BufferDistance20, ulong>.Start(_sRune)),
            21 => LevenshtomatonExecutionState.FromStruct(State<BitwiseBuffers.BufferDistance21, ulong>.Start(_sRune)),
            22 => LevenshtomatonExecutionState.FromStruct(State<BitwiseBuffers.BufferDistance22, ulong>.Start(_sRune)),
            23 => LevenshtomatonExecutionState.FromStruct(State<BitwiseBuffers.BufferDistance23, ulong>.Start(_sRune)),
            24 => LevenshtomatonExecutionState.FromStruct(State<BitwiseBuffers.BufferDistance24, ulong>.Start(_sRune)),
            25 => LevenshtomatonExecutionState.FromStruct(State<BitwiseBuffers.BufferDistance25, ulong>.Start(_sRune)),
            26 => LevenshtomatonExecutionState.FromStruct(State<BitwiseBuffers.BufferDistance26, ulong>.Start(_sRune)),
            27 => LevenshtomatonExecutionState.FromStruct(State<BitwiseBuffers.BufferDistance27, ulong>.Start(_sRune)),
            28 => LevenshtomatonExecutionState.FromStruct(State<BitwiseBuffers.BufferDistance28, ulong>.Start(_sRune)),
            29 => LevenshtomatonExecutionState.FromStruct(State<BitwiseBuffers.BufferDistance29, ulong>.Start(_sRune)),
            30 => LevenshtomatonExecutionState.FromStruct(State<BitwiseBuffers.BufferDistance30, ulong>.Start(_sRune)),
            _ => throw new NotSupportedException()
        };
    }

    private struct State<TBuffer, TNumber> : ILevenshtomatonExecutionState<State<TBuffer, TNumber>>
        where TBuffer : struct, BitwiseBuffers.IBuffer<TBuffer, TNumber>
        where TNumber : IBinaryInteger<TNumber>
    {
        private readonly Rune[] _sRune;
        private readonly int _sIndex;
        private TBuffer _states;
        private TBuffer _statesT;

        private State(Rune[] sRune, int sIndex)
        {
            _sRune = sRune;
            _sIndex = sIndex;
        }

        internal static State<TBuffer, TNumber> Start(Rune[] sRune)
        {
            var result = new State<TBuffer, TNumber>(sRune, 0);
            TBuffer.GetBuffer(ref result._states)[0] = TNumber.One;
            return result;
        }

        public bool MoveNext(Rune c, out State<TBuffer, TNumber> next)
        {
            Rune[] sRune = _sRune;
            int sIndex = _sIndex;

            int vectorLength = Math.Min(TBuffer.MaxVectorLength, sRune.Length - sIndex);

            TNumber vector = TNumber.Zero;
            TNumber flag = TNumber.One;
            foreach (var sChar in sRune.AsSpan(sIndex, vectorLength))
            {
                flag <<= 1;
                if (default(TCaseSensitivity).Equals(sChar, c))
                {
                    vector |= flag;
                }
            }

            TBuffer nextBuffer = default;
            TBuffer nextBufferT = default;

            Span<TNumber> states = TBuffer.GetBuffer(ref _states);
            Span<TNumber> statesT = TBuffer.GetBuffer(ref _statesT);
            Span<TNumber> nextStates = TBuffer.GetBuffer(ref nextBuffer);
            Span<TNumber> nextStatesT = TBuffer.GetBuffer(ref nextBufferT);

            TNumber nextState = (states[0] << 1) & vector;
            nextStates[0] = nextState;

            TNumber errorVector = states[0];
            TNumber deletionVector = errorVector;
            TNumber subsumptionMask = nextState >> 1 | nextState;
            TNumber combinedStates = nextState;

            for (int i = 1; i <= TBuffer.MaxError; i++)
            {
                nextStatesT[i] = (states[i] << 2) & vector;

                TNumber state = states[i];

                nextState =
                    (state << 1) & vector // regular matching on this row
                    | errorVector // insertion from prev row
                    | errorVector << 1 // substitution from prev row
                    | (deletionVector << 2) & vector // deletion from prev row
                    | (statesT[i] & (vector << 1)) // transposition
                    ;

                nextState = nextState & ~subsumptionMask;

                errorVector = state;
                deletionVector = (deletionVector << 1) | state;
                subsumptionMask = nextState >> 1 | nextState;
                combinedStates |= nextState;

                nextStates[i] = nextState & TBuffer.StateMask;
            }

            if (combinedStates != TNumber.Zero)
            {
                int offset = int.CreateTruncating(TNumber.TrailingZeroCount(combinedStates));

                for (int i = 0; i <= TBuffer.MaxError; i++)
                {
                    nextStates[i] >>= offset;
                }

                next = new State<TBuffer, TNumber>(_sRune, sIndex + offset);
                next._states = nextBuffer;
                next._statesT = nextBufferT;
                return true;
            }

            next = default;
            return false;
        }

        public bool IsFinal
        {
            get
            {
                var d = _sRune.Length - _sIndex;
                if (d <= TBuffer.MaxVectorLength)
                {
                    TNumber threshold = TNumber.One << d;

                    Span<TNumber> states = TBuffer.GetBuffer(ref _states);
                    TNumber finalVector = TNumber.Zero;
                    for (int i = 0; i <= TBuffer.MaxError; i++)
                    {
                        finalVector = (finalVector << 1) | states[i];
                    }

                    return finalVector >= threshold;
                }

                return false;
            }
        }

        public int Distance
        {
            get
            {
                var d = _sRune.Length - _sIndex;
                if (d <= TBuffer.MaxVectorLength)
                {
                    TNumber threshold = TNumber.One << d;

                    Span<TNumber> states = TBuffer.GetBuffer(ref _states);
                    TNumber finalVector = TNumber.Zero;
                    for (int i = 0; i <= TBuffer.MaxError; i++)
                    {
                        finalVector = (finalVector << 1) | states[i];
                        if (finalVector >= threshold)
                        {
                            return i;
                        }
                    }
                }

                throw new InvalidOperationException();
            }
        }
    }
}

#endif