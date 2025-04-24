namespace Lazy
{
    internal enum ReactionOnRepeatedDelayedDespawn
    {
        Ignore,
        ResetDelay,
        ResetDelayIfNewTimeIsLess,
        ResetDelayIfNewTimeIsGreater,
        ThrowException,
    }
}
