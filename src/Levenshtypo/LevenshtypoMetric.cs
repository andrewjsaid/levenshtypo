namespace Levenshtypo;

public enum LevenshtypoMetric
{
    /// <summary>
    /// Classic Levenshtein algorithm, allowing insertions, deletions and substitutions.
    /// </summary>
    Levenshtein = 0,

    /// <summary>
    /// Also known as "Optimal String Alignment Distance", this is a variant of Damerau-Levenshtein
    /// algorithm. It allows insertions, deletions, substitutions and transpositions however
    /// the transposed substring may not be modified again.
    /// e.g. the second edit is disallowed:
    ///   CA -> AC -> ABC
    /// </summary>
    RestrictedEdit = 1,

    // /// <summary>
    // /// Damerau-Levenshtein algorithm, allowing insertions, deletions, substitutions and transpositions.
    // /// This algorithm allows insertions after transposing, yielding unexpected results.
    // /// e.g. the following is allowed with distance 2:
    // ///   CA -> AC -> ABC
    // /// </summary>
    // DamerauLevenshtein = 2,
}
