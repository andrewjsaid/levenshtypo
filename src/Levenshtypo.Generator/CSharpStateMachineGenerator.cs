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

        var sb = new StringBuilder();
        sb.AppendLine(
            $$"""
            private readonly struct State : ILevenshtomatonExecutionState<State>
            {
            """);

        for (int i = 0; i <= maxDistance; i++)
        {
            var entriesPerState = 1 << i;
            var transitionPayload = new short[groupMap.Count * entriesPerState];
            Array.Fill(transitionPayload, (short)-1);
            var finalPayload = new bool[groupMap.Count];

            foreach (var state in states)
            {
                if (state.CharacteristicVectorLength == i)
                {
                    if (state.IsFinal)
                    {
                        finalPayload[groupMap[state.GroupId]] = true;
                    }

                    var stateTransitions = transitions.AsSpan(state.TransitionStartIndex, entriesPerState);
                    for (int v = 0; v < stateTransitions.Length; v++)
                    {
                        var transition = stateTransitions[v];
                        if (transition.MatchingStateStartIndex > -1)
                        {
                            var offset = (ushort)transition.IndexOffset;
                            var nextState = (ushort)groupMap[states[transition.MatchingStateStartIndex].GroupId];

                            transitionPayload[groupMap[state.GroupId] * entriesPerState + v] = (short)((offset << 8) | nextState);
                        }
                    }
                }
            }

            var encodedTransitionPayload = string.Join(", ", transitionPayload.Select(t => t == -1 ? "-1" : ("0x" + t.ToString("X2"))));
            var encodedFinalsPayload = string.Join(", ", finalPayload.Select(b => b.ToString().ToLower()));

            sb.AppendLine(
                $$"""
                    private static ReadOnlySpan<short> TransitionsD{{i}} => [{{encodedTransitionPayload}}];
                    private static ReadOnlySpan<bool> FinalsD{{i}} => [{{encodedFinalsPayload}}];
                """);
        }

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
                    var s = _sRune;
                    var sIndex = _sIndex;

                    short encodedNext;
                    
                    switch (s.Length - sIndex)
                    {
            """);

        for (int dKey = 0; dKey <= maxDistance; dKey++)
        {
            sb.AppendLine(
                $$"""
                            {{(dKey < maxDistance ? "case " + dKey.ToString() : "default")}}:
                            {
                """);

            var entriesPerState = 1 << dKey;

            sb.AppendLine(
                $$"""
                                var vector = 0
                """);

            for (int d = 1; d <= dKey; d++)
            {
                sb.AppendLine(
                    $$"""
                                        | (default(TCaseSensitivity).Equals(c, s[sIndex + {{d - 1}}]) ? {{1 << (dKey - d)}} : 0)
                    """);
            }

            sb.AppendLine(
                $$"""
                                    ;
                                
                                encodedNext = TransitionsD{{dKey}}[_state * {{entriesPerState}} + vector];

                                break;
                            }
                """);
        }

        sb.AppendLine(
            """
                    }
                                        
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
                    (_sRune.Length - _sIndex) switch
                    {
            """);

        for (int dKey = 0; dKey <= maxDistance; dKey++)
        {
            var key = dKey == maxDistance ? "_" : dKey.ToString();
            sb.AppendLine(
                $$"""
                            {{key}} => FinalsD{{dKey}}[_state],
                """);
        }

        sb.AppendLine(
            """
                    };
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
