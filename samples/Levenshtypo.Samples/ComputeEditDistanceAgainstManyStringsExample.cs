namespace Levenshtypo.Samples
{
    /// <summary>
    /// Illustrates how to calculate Levenshtein distance against
    /// many words faster than using Dynamic Programming
    /// </summary>
    public class ComputeEditDistanceAgainstManyStringsExample
    {
        // Benchmarks below show that a naive implementation,
        // even if it is well written, is 10x slower than using
        // an automaton.
        // Benchmark run against English language dataset.
        //
        // | Method          | Mean       | Error     | StdDev    | Allocated |
        // |-----------------|-----------:|----------:|----------:|----------:|
        // | Using_naive     | 103.190 ms | 1.4706 ms | 1.3756 ms |     214 B |
        // | Using_automaton |   8.161 ms | 0.0469 ms | 0.0439 ms |      12 B |

        public string[] Search(string searchWord, string[] against)
        {
            var automaton = LevenshtomatonFactory.Instance.Construct(searchWord, maxEditDistance: 2);

            var results = new List<string>();

            foreach (var word in against)
            {
                // Naive version would be:
                // bool matches = LevenshteinDistance.Levenshtein(searchWord, word) <= 2;

                // Automaton version is:
                bool matches = automaton.Matches(word);
                if (matches)
                {
                    results.Add(word);
                }
            }

            return results.ToArray();
        }

    }
}
