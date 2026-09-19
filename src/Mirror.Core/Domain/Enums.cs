namespace Mirror.Core.Domain;

public enum TrackingState
{
    Running,
    Paused,
    Idle,
    Locked,
    Unavailable,
    Stopped
}

public enum SessionCloseReason
{
    AppSwitch,
    Idle,
    Lock,
    Sleep,
    TrackingDisabled,
    ProcessExit,
    Shutdown,
    RecoveredAfterCrash
}

public enum InferenceBackendKind
{
    Qnn,
    DirectMl,
    Cpu
}

public enum BehavioralPatternType
{
    Normal,
    HighSwitchingBurst,
    ExtendedSingleAppSession,
    LateNightUsageSpike,
    RapidReopenPattern,
    CompositeScrollLike
}
