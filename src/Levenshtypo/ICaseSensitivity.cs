using System.Globalization;
using System.Text;

namespace Levenshtypo;

internal interface ICaseSensitivity<TSelf> where TSelf : struct, ICaseSensitivity<TSelf>
{
    bool Equals(Rune a, Rune b);

    bool Equals(char a, char b);
}

internal struct CaseSensitive : ICaseSensitivity<CaseSensitive>
{
    public bool Equals(Rune a, Rune b) => a == b;

    public bool Equals(char a, char b) => a == b;
}

internal struct CaseInsensitive : ICaseSensitivity<CaseInsensitive>
{
    public bool Equals(Rune a, Rune b) => Rune.ToLower(a, CultureInfo.InvariantCulture) == Rune.ToLower(b, CultureInfo.InvariantCulture);

    public bool Equals(char a, char b) => char.ToLower(a, CultureInfo.InvariantCulture) == char.ToLower(b, CultureInfo.InvariantCulture);
}
