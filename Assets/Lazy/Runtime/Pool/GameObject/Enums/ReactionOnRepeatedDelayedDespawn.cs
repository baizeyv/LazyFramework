namespace Lazy.Pool.GameObject.Enums
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
