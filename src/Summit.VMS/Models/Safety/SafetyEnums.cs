namespace Summit.VMS.Models.Safety;

/// <summary>Lifecycle of a distress incident. Drives staged visibility.</summary>
public enum IncidentStatus
{
    /// <summary>Victim triggered the distress signal; nothing verified yet.</summary>
    Raised = 0,

    /// <summary>Automated verification calls placed to emergency contacts.</summary>
    UnderVerification = 1,

    /// <summary>Contacts confirmed the person is genuinely missing/in danger.</summary>
    Confirmed = 2,

    /// <summary>An FIR record was auto-drafted from the confirmed incident.</summary>
    FirRegistered = 3,

    /// <summary>Escalated to higher authorities for action.</summary>
    Escalated = 4,

    /// <summary>Closed after resolution.</summary>
    Resolved = 5,

    /// <summary>Contacts indicated this was a mistaken/accidental trigger.</summary>
    FalseAlarm = 6,

    /// <summary>Victim cancelled within the grace window.</summary>
    Cancelled = 7
}

/// <summary>Per-contact response during the initial check.</summary>
public enum VerificationDecision
{
    Pending = 0,
    ConfirmedMissing = 1,
    DeniedFalseAlarm = 2,
    NoResponse = 3
}

public enum ContactRelation
{
    Other = 0,
    Mother = 1,
    Father = 2,
    Guardian = 3,
    Sibling = 4,
    Friend = 5
}

/// <summary>State of a victim's self-registration in the mobile app.</summary>
public enum RegistrationStatus
{
    PendingVerification = 0,
    PendingContactConsent = 1,
    PendingVoiceEnrollment = 2,
    Active = 3,
    Suspended = 4
}

/// <summary>
/// Outcome of Summit identity verification. This is a pluggable abstraction —
/// the current implementation issues a token via a mock; a real verifier (phone
/// OTP, document/KYC, or a licensed provider) can be slotted in behind the same
/// interface without other code changes.
/// </summary>
public enum VerificationStatus
{
    NotVerified = 0,
    Verified = 1,
    Failed = 2,
    Pending = 3
}

/// <summary>States this deployment covers. Extend as the rollout grows.</summary>
public enum IndianState
{
    Telangana = 0,
    AndhraPradesh = 1
}
