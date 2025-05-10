using System.Text;

namespace Levenshtypo;

internal struct PrefixTrackingLevenshtomatonExecutionState<TInner>
    : ILevenshtomatonExecutionState<PrefixTrackingLevenshtomatonExecutionState<TInner>>
    where TInner : ILevenshtomatonExecutionState<TInner>
{
    private const int StopFlag = 1 << 31;
    private const int MatchesFlag = 1 << 30;
    private const int DistanceFlag = -1 & ~(StopFlag | MatchesFlag);

    private TInner _inner;
    private int _state;

    private PrefixTrackingLevenshtomatonExecutionState(TInner inner, int distance)
    {
        _inner = inner;
        _state = distance;
    }

    internal static PrefixTrackingLevenshtomatonExecutionState<TInner> Start(TInner inner)
    {
        var state = !inner.IsFinal
            ? DistanceFlag
            : MatchesFlag | inner.Distance;
        return new(inner, state);
    }

    public bool MoveNext(Rune c, out PrefixTrackingLevenshtomatonExecutionState<TInner> next)
    {
        var state = _state;

        if ((state & StopFlag) != 0)
        {
            next = this;
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
                }
            }

            next = new(nextState, state);
            return true;

        }
        else
        {
            state |= StopFlag;
            next = new(default!, state);
            return (state & MatchesFlag) != 0;
        }
    }

    public readonly bool IsFinal => (_state & MatchesFlag) != 0;

    public readonly int Distance => _state & DistanceFlag;
}
