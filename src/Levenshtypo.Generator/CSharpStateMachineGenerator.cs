using System.Runtime.CompilerServices;
using System.Text;
using DfaState = Levenshtypo.ParameterizedLevenshtomaton.DfaState;
using DfaTransition = Levenshtypo.ParameterizedLevenshtomaton.DfaTransition;
using Template = Levenshtypo.ParameterizedLevenshtomaton.Template;

namespace Levenshtypo.Generator;

internal static class CSharpStateMachineGenerator
{
    public static string WriteCSharpStateMachine(int distance, LevenshtypoMetric metric)
    {
        var template = ParameterizedLevenshtomaton.CreateTemplate(distance, metric);
        ref var states = ref GetStates(template);
        ref var transitions = ref GetTransitions(template);
        return WriteCSharpStateMachine(distance, states, transitions);
    }

    private static string WriteCSharpStateMachine(int distance, DfaState[] states, DfaTransition[] transitions)
    {
        var maxDistance = (2 * distance) + 1;

        var records = new List<TransitionRecord>();

        var groupMap = new Dictionary<int, int>();

        foreach (var state in states)
        {
            if (!groupMap.ContainsKey(state.GroupId))
            {
                groupMap.Add(state.GroupId, groupMap.Count);
            }
        }

        if (groupMap.Count > 64)
        {
            throw new NotSupportedException("Finals uses a ulong to represent final state");
        }

        var transitionsData = new List<short>();
        var distanceData = new List<byte>();

        for (int dKey = 0; dKey <= maxDistance; dKey++)
        {
            var transitionEntriesPerState = 1 << dKey;
            var transitionPayload = new short[groupMap.Count * transitionEntriesPerState];
            Array.Fill(transitionPayload, (short)-1);

            var distancePayload = new byte[groupMap.Count];
            Array.Fill(distancePayload, (byte)(~0 & 0xFF));

            foreach (var state in states)
            {
                if (state.CharacteristicVectorLength == dKey)
                {
                    distancePayload[groupMap[state.GroupId]] = (byte)(~state.FinalErrorNegated & 0xFF);

                    var stateTransitions = transitions.AsSpan(state.TransitionStartIndex, transitionEntriesPerState);
                    for (int v = 0; v < stateTransitions.Length; v++)
                    {
                        var transition = stateTransitions[v];
                        if (transition.MatchingStateStartIndex > -1)
                        {
                            var offset = (ushort)transition.IndexOffset;
                            var nextState = (ushort)groupMap[states[transition.MatchingStateStartIndex].GroupId];

                            transitionPayload[groupMap[state.GroupId] * transitionEntriesPerState + v] = (short)((offset << 8) | nextState);
                        }
                    }
                }
            }

            transitionsData.AddRange(transitionPayload);
            distanceData.AddRange(distancePayload);
        }

        var sb = new StringBuilder();
        sb.AppendLine(
            $$"""
            private readonly struct State : ILevenshtomatonExecutionState<State>
            {
                private static ReadOnlySpan<short> TransitionsData => [{{string.Join(", ", transitionsData.Select(t => t == -1 ? "-1" : ("0x" + t.ToString("X2"))))}}];
                private static ReadOnlySpan<byte> DistanceData => [{{string.Join(", ", distanceData.Select(t => "0x" + t.ToString("X2")))}}];
            """);

        sb.AppendLine(
            $$"""

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
                                        
                    var vectorLength = Math.Min({{maxDistance}}, sRune.Length - sIndex);
                    
                    var vector = 0;
                    foreach (var sChar in sRune.AsSpan(sIndex, vectorLength))
                    {
                        vector <<= 1;
                        if (default(TCaseSensitivity).Equals(sChar, c))
                        {
                            vector |= 1;
                        }
                    }

                    var dStart = {{groupMap.Count}} * ((1 << vectorLength) - 1);
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
            """);

        for (int dKey = 0; dKey <= maxDistance; dKey++)
        {
            var key = dKey == maxDistance ? "_" : dKey.ToString();

            ulong bitVector = 0;
            foreach (var state in states)
            {
                if (state.CharacteristicVectorLength == dKey)
                {
                    if (state.FinalErrorNegated != 0)
                    {
                        bitVector |= 1ul << groupMap[state.GroupId];
                    }
                }
            }

            sb.AppendLine(
                $$"""
                            {{key}} => 0x{{bitVector.ToString("X2")}}ul,
                """);
        }

        sb.AppendLine(
            $$"""
                    });
                
                public int Distance => DistanceData[Math.Min({{maxDistance}}, _sRune.Length - _sIndex) * {{groupMap.Count}} + _state];
            }
            """);

        return sb.ToString();
    }

    private record TransitionRecord(
        int Distance,
        int FromState,
        uint Vector,
        int ToState,
        int ReadOffset
        );

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_states")]
    private static extern ref DfaState[] GetStates(Template template);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_transitions")]
    private static extern ref DfaTransition[] GetTransitions(Template template);

}
