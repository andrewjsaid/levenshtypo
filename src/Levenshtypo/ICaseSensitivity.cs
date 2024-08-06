using System;
using System.Collections.Generic;

namespace Levenshtypo;
internal interface ICaseSensitivity<TSelf> where TSelf : struct, ICaseSensitivity<TSelf>
{
    bool Equals(char a, char b);

    char Normalize(char a);

    IComparer<string> KeyComparer { get; }
}

internal struct CaseSensitive : ICaseSensitivity<CaseSensitive>
{
    public bool Equals(char a, char b) => a == b;

    public char Normalize(char a) => a;

    public IComparer<string> KeyComparer => StringComparer.Ordinal;
}

internal struct CaseInsensitive : ICaseSensitivity<CaseInsensitive>
{
    public bool Equals(char a, char b) => char.ToLower(a) == char.ToLower(b);

    public char Normalize(char a) => char.ToLower(a);

    public IComparer<string> KeyComparer => StringComparer.OrdinalIgnoreCase;
}
