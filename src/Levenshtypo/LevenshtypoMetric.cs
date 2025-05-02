namespace Levenshtypo;

/// <summary>
/// Specifies the string distance algorithm used to compare input strings against
/// a reference string in a <see cref="Levenshtomaton"/>.
/// </summary>
public enum LevenshtypoMetric
{
    /// <summary>
    /// Levenshtein distance — the classic edit distance algorithm that allows insertions, deletions,
    /// and substitutions of single characters.
    /// </summary>
    Levenshtein = 0,
    
    /// <summary>
    /// Restricted edit distance, also known as Optimal String Alignment (OSA) distance.
    /// This variant of Damerau-Levenshtein allows insertions, deletions, substitutions,
    /// and adjacent character transpositions. However, it does not permit edits on characters
    /// that were involved in a previous transposition.
    ///
    /// <para>
    /// For example, the following sequence is not permitted in OSA:
    /// <c>CA → AC → ABC</c>, because the character involved in the transposition (<c>C</c> and <c>A</c>)
    /// cannot participate in a subsequent edit.
    /// </para>
    /// </summary>
    RestrictedEdit = 1,

    // 🚧 Optional for future:
    // /// <summary>
    // /// Damerau-Levenshtein distance — allows insertions, deletions, substitutions,
    // /// and unrestricted adjacent transpositions, including those on characters already edited.
    // /// This variant permits sequences such as <c>CA → AC → ABC</c> with distance 2.
    // /// </summary>
    // DamerauLevenshtein = 2,
}
