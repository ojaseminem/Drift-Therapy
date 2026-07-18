public enum RunState
{
    Ready,
    Running,
    /// <summary>Crash physics are playing out (controls disabled) before the run actually ends.</summary>
    Crashing,
    Failed,
    Reviving,
    Restarting
}
