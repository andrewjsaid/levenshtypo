using Levenshtypo.Generator;

// Bencmarks show that the below are actually less efficient than
// the parameterized versions.
var d0 = CSharpStateMachineGenerator.WriteCSharpStateMachine(0);
var d1 = CSharpStateMachineGenerator.WriteCSharpStateMachine(1);
var d2 = CSharpStateMachineGenerator.WriteCSharpStateMachine(2);

Console.ReadLine();
