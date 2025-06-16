using System.Text;

namespace Levenshtypo;

internal struct PrefixTrackingLevenshtomatonExecutionState<TInner>
    : ILevenshtomatonExecutionState<PrefixTrackingLevenshtomatonExecutionState<TInner>>
    where TInner : ILevenshtomatonExecutionState<TInner>
{
    private const int StopFlag = 1 << 31;
    private const int MatchesFlag = 1 << 30;
    private const int DistanceFlag = -1 & ~(StopFlag | MatchesFlag);

    private readonly TInner _inner;
    private readonly int _state;
    private readonly int _lengthSoFar;
    private readonly int _suffixLength;

    private PrefixTrackingLevenshtomatonExecutionState(TInner inner, int distance, int lengthSoFar, int suffixLength)
    {
        _inner = inner;
        _state = distance;
        _lengthSoFar = lengthSoFar;
        _suffixLength = suffixLength;
    }

    internal static PrefixTrackingLevenshtomatonExecutionState<TInner> Start(TInner inner)
    {
        var state = !inner.IsFinal
            ? DistanceFlag
            : MatchesFlag | inner.Distance;
        return new(inner, state, lengthSoFar: 0, suffixLength: 0);
    }

    public bool MoveNext(Rune c, out PrefixTrackingLevenshtomatonExecutionState<TInner> next)
    {
        var state = _state;
        var suffixLength = _suffixLength;

        if ((state & StopFlag) != 0)
        {
            next = new(_inner, state, _lengthSoFar + 1, suffixLength + 1);
            return (state & MatchesFlag) != 0;
        }

        if (_inner.MoveNext(c, out var nextState))
        {
            if (nextState.IsFinal)
            {
                var newDistance = nextState.Distance;
                if ((state & DistanceFlag) > newDistance)
                {
                    state = MatchesFlag | newDistance;
                    suffixLength = -1;
                }
            }

            next = new(nextState, state, _lengthSoFar + 1, suffixLength + 1);
            return true;

        }
        else
        {
            state |= StopFlag;
            next = new(default!, state, _lengthSoFar + 1, suffixLength + 1);
            return (state & MatchesFlag) != 0;
        }
    }

    public readonly bool IsFinal => (_state & MatchesFlag) != 0;

    public readonly int Distance => _state & DistanceFlag;

    bool ILevenshtomatonExecutionState<PrefixTrackingLevenshtomatonExecutionState<TInner>>.TryGetPrefixSearchMetadata(out int metadata)
    {
        metadata = PrefixMetadataUtils.EncodeMetadata(_lengthSoFar - _suffixLength, _suffixLength);
        return true;
    }
}

internal static class PrefixMetadataUtils
{
    internal static int EncodeMetadata(int prefixLength, int suffixLength) => ((prefixLength & 0xFFFF) << 16) | (suffixLength & 0xFFFF);

    internal static void DecodeMetadata(int metadata, out int prefixLength, out int suffixLength)
    {
        prefixLength = metadata >> 16;
        suffixLength = metadata & 0xFFFF;
    }
}