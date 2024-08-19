using System.Diagnostics;
using System.Runtime.CompilerServices;
using Levenshtypo;
using Levenshtypo.Generator;

using DfaState = Levenshtypo.ParameterizedLevenshtomaton.DfaState;
using DfaTransition = Levenshtypo.ParameterizedLevenshtomaton.DfaTransition;
using Template = Levenshtypo.ParameterizedLevenshtomaton.Template;

// Benchmarks show that the below are actually less efficient than
// the parameterized versions.
var dLev1 = CSharpStateMachineGenerator.WriteCSharpStateMachine(1, LevenshtypoMetric.Levenshtein);
var dLev2 = CSharpStateMachineGenerator.WriteCSharpStateMachine(2, LevenshtypoMetric.Levenshtein);
var dRe1 = CSharpStateMachineGenerator.WriteCSharpStateMachine(1, LevenshtypoMetric.RestrictedEdit);
var dRe2 = CSharpStateMachineGenerator.WriteCSharpStateMachine(2, LevenshtypoMetric.RestrictedEdit);

Console.WriteLine("Press any key to analyze state tables");
Console.ReadLine();

// Proof that the state transitions can be calculated on demand for distances less than MaxEditDistance.
//   i.e. can avoid computing transitions for each distance below MaxEditDistance.
// Could be implemented like this, if necessary.

var lev3 = ParameterizedLevenshtomaton.CreateTemplate(3, LevenshtypoMetric.Levenshtein);
var states = GetRef.GetStates(lev3);
var transitions = GetRef.GetTransitions(lev3);

foreach (var sGroup in states.GroupBy(s => s.GroupId))
{
    var gStates = sGroup.OrderByDescending(s => s.CharacteristicVectorLength).ToArray();

    var dMaxTransitions = transitions.AsSpan(gStates[0].TransitionStartIndex, 1 << gStates[0].CharacteristicVectorLength);

    foreach (var state in sGroup.Skip(1))
    {
        var dTransitions = transitions.AsSpan(state.TransitionStartIndex, 1 << state.CharacteristicVectorLength);

        var shift = gStates[0].CharacteristicVectorLength - state.CharacteristicVectorLength;

        for (int tIndex = 0; tIndex < dTransitions.Length; tIndex++)
        {
            var dMaxTransition = dMaxTransitions[tIndex << shift];
            var dTransition = dTransitions[tIndex];

            if (dTransition.IndexOffset != dMaxTransition.IndexOffset)
            {
                Debugger.Break();
            }

            if (dTransition.MatchingStateStartIndex == -1 || dMaxTransition.MatchingStateStartIndex == -1)
            {
                if (dTransition.MatchingStateStartIndex != dMaxTransition.MatchingStateStartIndex)
                {
                    Debugger.Break();
                }

                continue;
            }

            var maxState = states[dMaxTransition.MatchingStateStartIndex];
            var dState = states[dTransition.MatchingStateStartIndex];

            var dName = ParseName(dState.Name);
            var dRenamed = string.Join(' ', dName.OrderBy(x => x.c).ThenBy(x => x.e));

            var maxName = ParseName(maxState.Name);
            maxName = maxName.Where(x => x.c <= state.CharacteristicVectorLength - dTransition.IndexOffset).ToArray();
            var maxRenamed = string.Join(' ', maxName.OrderBy(x => x.c).ThenBy(x => x.e));

            if (dRenamed != maxRenamed)
            {
                Debugger.Break();
            }
        }

    }
}

static (int c, int e)[] ParseName(string name)
{
    var parts = name.Split(' ');
    return parts
        .Where(p => p[0] == 'c')
        .Select(x =>
        {
            var p = x.Split('_');
            return (int.Parse(p[0][1..]), int.Parse(p[1][1..]));
        }).ToArray();
}

static class GetRef
{
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_states")]
    public static extern ref DfaState[] GetStates(Template template);

    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_transitions")]
    public static extern ref DfaTransition[] GetTransitions(Template template);
}