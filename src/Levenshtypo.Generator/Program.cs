using Levenshtypo;
using Levenshtypo.Generator;

// Benchmarks show that the below are actually less efficient than
// the parameterized versions.
var d0 = CSharpStateMachineGenerator.WriteCSharpStateMachine(0, LevenshtypoMetric.Levenshtein);
var d1 = CSharpStateMachineGenerator.WriteCSharpStateMachine(1, LevenshtypoMetric.Levenshtein);
var d2 = CSharpStateMachineGenerator.WriteCSharpStateMachine(2, LevenshtypoMetric.Levenshtein);

Console.ReadLine();
