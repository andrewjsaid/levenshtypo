using BenchmarkDotNet.Attributes;
using Levenshtypo;

public class LevenshtomatonFactoryTests
{
    [Benchmark]
    public object Create0() => new LevenshtomatonFactory().Construct(DataHelpers.Initiate, 0);

    [Benchmark]
    public object Create1() => new LevenshtomatonFactory().Construct(DataHelpers.Initiate, 1);

    [Benchmark]
    public object Create2() => new LevenshtomatonFactory().Construct(DataHelpers.Initiate, 2);

    [Benchmark]
    public object Create3() => new LevenshtomatonFactory().Construct(DataHelpers.Initiate, 3);
}
