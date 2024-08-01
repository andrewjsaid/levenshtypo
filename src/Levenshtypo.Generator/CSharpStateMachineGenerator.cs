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
        int MapGroup(int index)
        {
            if (!groupMap.TryGetValue(index, out var result))
            {
                groupMap.Add(index, result = groupMap.Count);
            }
            return result;
        }

        var finalStates = new List<(int state, int vLen)>();

        foreach (var state in states)
        {
            if (state.IsFinal)
            {
                finalStates.Add((MapGroup(state.GroupId), state.CharacteristicVectorLength));
            }

            var numTransitions = 1 << state.CharacteristicVectorLength;
            var stateTransitions = transitions.AsSpan(state.TransitionStartIndex, numTransitions);

            for (int characteristicVector = 0; characteristicVector < stateTransitions.Length; characteristicVector++)
            {
                var stateTransition = stateTransitions[characteristicVector];

                records.Add(new TransitionRecord(
                Distance: state.CharacteristicVectorLength,
                FromState: MapGroup(state.GroupId),
                Vector: (uint)characteristicVector,
                ToState: stateTransition.MatchingStateStartIndex == -1 ? -1 : MapGroup(states[stateTransition.MatchingStateStartIndex].GroupId),
                ReadOffset: stateTransition.IndexOffset));
            }
        }


        var sb = new StringBuilder();
        sb.AppendLine(
            $$"""
            private readonly struct State : ILevenshtomatonExecutionState<State>
            {
              private readonly string _s;
              private readonly int _sIndex;
              private readonly int _state;

              internal State(string s, int sIndex, int state)
              {
                _s = s;
                _sIndex = sIndex;
                _state = state;
              }

              public bool MoveNext(char c, out State next)
              {
                var s = _s;
                var sIndex = _sIndex;
                var state = _state;
                
                var d = s.Length - sIndex;
                
                int nextState = -1;
                int offset = 0;

                switch (d)
                {
            """);

        foreach (var gDistance in records.GroupBy(r => r.Distance).OrderBy(g => g.Key))
        {
            if (gDistance.All(x => x.ToState == -1))
                continue;

            sb.AppendLine(
                $$"""
                      {{(gDistance.Key < maxDistance ? "case " + gDistance.Key.ToString() : "default")}}:
                      {
                """);

            if (gDistance.Key > 0)
            {
                sb.AppendLine(
                    """
                            var vector = 0
                    """);

                for (int d = 0; d < gDistance.Key; d++)
                {
                    sb.AppendLine(
                        $$"""
                                  | ((default(TCaseSensitivity).Equals(c, s[sIndex + {{d}}]) ? 1 : 0) << {{gDistance.Key - d - 1}})
                        """);
                }

                sb.AppendLine(
                    "          ;");
            }

            sb.AppendLine(
                $$"""
                        switch (state)
                        {
                """);

            foreach (var gState in gDistance.GroupBy(r => r.FromState).OrderBy(g => g.Key))
            {
                if (gState.All(x => x.ToState == -1))
                    continue;

                sb.AppendLine(
                    $$"""
                              case {{gState.Key}}:
                              {
                    """);

                if (gDistance.Key == 0)
                {
                    if (gState.Count() > 1)
                    {
                        throw new InvalidOperationException("At distance 0 there should be no switching on vector");
                    }

                    if (gState.Single().ReadOffset != 0)
                    {
                        throw new InvalidOperationException("At distance 0 you shouldn't be reading further");
                    }

                    sb.AppendLine(
                        $$"""
                                    nextState = {{gState.Single().ToState}};
                        """);
                }
                else
                {

                    sb.AppendLine(
                        $$"""
                                    switch (vector)
                                    {
                        """);

                    foreach (var byVector in gState)
                    {
                        if (byVector.ToState != -1)
                        {
                            sb.AppendLine(
                                $$"""
                                              case {{byVector.Vector}}:
                                              {
                                                nextState = {{byVector.ToState}};
                                                offset = {{byVector.ReadOffset}};
                                                break;
                                              }
                                """);
                        }
                    }

                    sb.AppendLine(
                        $$"""
                                    }
                        """);
                }

                sb.AppendLine(
                    $$"""
                                break;
                              }
                    """);
            }

            sb.AppendLine(
                $$"""
                        }
                  
                        break;
                      }
                """);
        }

        sb.AppendLine(
            """
                }

                if (nextState >= 0)
                {
                    next = new State(_s, sIndex + offset, nextState);
                    return true;
                }
                
                next = default;
                return false;
              }

              public bool IsFinal
              {
                get
                {
            """);


        sb.AppendLine(
            $$"""
                var state = _state;
                var d = _s.Length - _sIndex;
                
                switch (state)
                {
            """);

        foreach (var gState in finalStates.GroupBy(x => x.state))
        {
            sb.AppendLine(
                $$"""
                      case {{gState.Key}}:
                      {
                        switch (d)
                        {
                """);

            foreach (var (_, d) in gState)
            {
                sb.AppendLine(
                    $$"""
                              case {{d}}:
                    """);
            }

            sb.AppendLine(
                $$"""
                            return true;
                        }

                        break;
                      }
                """);
        }

        sb.AppendLine(
            """
                  }

                  return false;
                }
              }
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
