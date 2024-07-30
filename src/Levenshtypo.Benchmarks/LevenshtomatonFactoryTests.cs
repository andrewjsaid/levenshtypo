using BenchmarkDotNet.Attributes;
using Levenshtypo;

public class LevenshtomatonFactoryTests
{
    private const string SearchWord = "initiate";

    [Benchmark]
    public object Distance0_Create() => new LevenshtomatonFactory().Construct(SearchWord, 0);

    [Benchmark]
    public object Distance1_Create() => new LevenshtomatonFactory().Construct(SearchWord, 1);

    [Benchmark]
    public object Distance2_Create() => new LevenshtomatonFactory().Construct(SearchWord, 2);

    [Benchmark]
    public object Distance3_Create() => new LevenshtomatonFactory().Construct(SearchWord, 3);
}
