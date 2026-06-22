using Summit.VMS.Models.Safety;

namespace Summit.VMS.Services.Safety;

// ===========================================================================
//  External integration boundaries.
//
//  These interfaces isolate the three integrations that CANNOT be implemented
//  with self-contained code because they require licensed third parties:
//
//   * ISummitVerifier   -> identity verification. The mock simply issues a token;
//                          swap for phone-OTP / KYC / a licensed provider later.
//   * IVerificationCaller -> a telephony/IVR provider (Exotel/Twilio/Knowlarity)
//                            to place the automated initial-check calls.
//   * IVoiceMatchService -> a speaker-verification / keyword service to enrol
//                            voice samples and score a live distress trigger.
//
//  The *Mock* implementations below let the whole workflow run end-to-end in
//  development. They are NOT safe for production and must be replaced with real
//  provider clients. See DESIGN.md.
// ===========================================================================

public record SummitVerificationOutcome(VerificationStatus Status, string? Token);

public interface ISummitVerifier
{
    /// <summary>
    /// Verify a victim's identity and return an opaque token. The optional
    /// <paramref name="code"/> is a challenge (e.g. a phone OTP) a real verifier
    /// might require; the mock ignores it.
    /// </summary>
    Task<SummitVerificationOutcome> VerifyAsync(VictimProfile victim, string? code = null);
}

public record CallResult(bool Placed, string? CallReference);

public interface IVerificationCaller
{
    /// <summary>Place an informational/consent call to a newly-added contact.</summary>
    Task<CallResult> PlaceConsentCallAsync(EmergencyContact contact, VictimProfile victim);

    /// <summary>
    /// Place an automated initial-check call asking the contact to confirm
    /// whether the victim is genuinely missing. The IVR result is delivered
    /// asynchronously via webhook to the API (see PoliceApp/VictimApp callback).
    /// </summary>
    Task<CallResult> PlaceVerificationCallAsync(EmergencyContact contact, DistressIncident incident);
}

public record VoiceMatch(double Score, string? VoiceprintId);

public interface IVoiceMatchService
{
    /// <summary>Enrol one sample, returning a voiceprint id.</summary>
    Task<string> EnrollAsync(Stream audio);

    /// <summary>Score a live trigger sample against the victim's enrolled prints (0..1).</summary>
    Task<VoiceMatch> MatchAsync(Stream audio, IEnumerable<string> enrolledVoiceprintIds);
}

public interface IOtpSender
{
    /// <summary>
    /// Send a one-time code to a phone. The DEV mock returns the code so the
    /// prototype can show it; a real SMS provider returns null and texts the user.
    /// </summary>
    Task<(string Code, string? DevCode)> SendAsync(string phone);
}

// ---------------------------------------------------------------------------
//  DEVELOPMENT-ONLY MOCKS
// ---------------------------------------------------------------------------

/// <summary>DEV ONLY. Issues a verification token and marks the profile verified.</summary>
public class MockSummitVerifier : ISummitVerifier
{
    public Task<SummitVerificationOutcome> VerifyAsync(VictimProfile victim, string? code = null)
        => Task.FromResult(new SummitVerificationOutcome(
            VerificationStatus.Verified,
            Token: $"SVT-{Guid.NewGuid():N}"));
}

/// <summary>DEV ONLY. Logs the "call" and returns a fake reference.</summary>
public class MockVerificationCaller : IVerificationCaller
{
    private readonly ILogger<MockVerificationCaller> _log;
    public MockVerificationCaller(ILogger<MockVerificationCaller> log) => _log = log;

    public Task<CallResult> PlaceConsentCallAsync(EmergencyContact contact, VictimProfile victim)
    {
        _log.LogInformation("[MOCK] Consent call to {Name} ({Phone}) re victim {Victim}.",
            contact.Name, contact.Phone, victim.FullName);
        return Task.FromResult(new CallResult(true, $"mock-consent-{Guid.NewGuid():N}"));
    }

    public Task<CallResult> PlaceVerificationCallAsync(EmergencyContact contact, DistressIncident incident)
    {
        _log.LogInformation("[MOCK] Verification call to {Name} ({Phone}) for incident {Id}.",
            contact.Name, contact.Phone, incident.Id);
        return Task.FromResult(new CallResult(true, $"mock-verify-{Guid.NewGuid():N}"));
    }
}

/// <summary>DEV ONLY. Returns a fixed high score so the flow proceeds.</summary>
public class MockVoiceMatchService : IVoiceMatchService
{
    public Task<string> EnrollAsync(Stream audio) =>
        Task.FromResult(Guid.NewGuid().ToString("N"));

    public Task<VoiceMatch> MatchAsync(Stream audio, IEnumerable<string> enrolledVoiceprintIds) =>
        Task.FromResult(new VoiceMatch(0.92, enrolledVoiceprintIds.FirstOrDefault()));
}

/// <summary>DEV ONLY. Generates a 6-digit OTP and returns it instead of texting.</summary>
public class MockOtpSender : IOtpSender
{
    private readonly ILogger<MockOtpSender> _log;
    public MockOtpSender(ILogger<MockOtpSender> log) => _log = log;

    public Task<(string Code, string? DevCode)> SendAsync(string phone)
    {
        var code = Random.Shared.Next(100000, 999999).ToString();
        _log.LogInformation("[MOCK] OTP {Code} -> {Phone}", code, phone);
        return Task.FromResult((code, (string?)code)); // DevCode surfaced for the prototype
    }
}
