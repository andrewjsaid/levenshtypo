using Levenshtypo;
using Levenshtypo.Generator;

// Benchmarks show that the below are actually less efficient than
// the parameterized versions.
var dLev1 = CSharpStateMachineGenerator.WriteCSharpStateMachine(1, LevenshtypoMetric.Levenshtein);
var dLev2 = CSharpStateMachineGenerator.WriteCSharpStateMachine(2, LevenshtypoMetric.Levenshtein);
var dRe1 = CSharpStateMachineGenerator.WriteCSharpStateMachine(1, LevenshtypoMetric.RestrictedEdit);
var dRe2 = CSharpStateMachineGenerator.WriteCSharpStateMachine(2, LevenshtypoMetric.RestrictedEdit);

Console.ReadLine();
