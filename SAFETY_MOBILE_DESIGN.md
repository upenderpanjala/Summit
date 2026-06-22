# Summit Safety — Mobile Apps & Distress Workflow (Design)

**Deployment scope:** Telangana and Andhra Pradesh (state → district → zone routing).

This document describes two mobile apps — a **Victim app** and a **Police app** —
and the backend workflow that connects them to the existing Summit VMS platform.
It is deliberately explicit about which parts are working prototype code and which
parts depend on licensed third-party services that cannot be self-contained.

> ⚠️ **Life-safety disclaimer.** This is a prototype. A real deployment that people
> rely on in danger must not ship until the integrations below are implemented with
> production-grade providers, audited for security, cleared legally, and tested for
> reliability (including offline, low-battery, and false-trigger cases). Voice
> detection must never be the *only* way to raise an alarm.

---

## 1. The end-to-end workflow

```
VICTIM APP                         BACKEND                          POLICE APP
----------                         -------                          ----------
Register details ───────────────▶  VictimProfile (PendingVerification)
Summit verify ──────────────────▶  issue opaque token → mark Verified
Add 3+ contacts ────────────────▶  EmergencyContacts + consent calls
Enrol voice ×3 ─────────────────▶  Voiceprints (Active)

   ── in danger ──
Send distress voice + location ─▶  DistressIncident: Raised
                                   auto-place verification calls
                                   Status → UnderVerification ──────▶ Pending queue
                                                                      (station officer
                                                                       runs initial check)
Parent confirms (IVR/officer) ◀─▶  ContactVerification
Friend confirms (IVR/officer) ◀─▶  ContactVerification
                                   rule met → Status: Confirmed
                                   auto-draft FIR (MissingPerson)
                                   Status → FirRegistered
                                   escalate (notify hierarchy)
                                   Status → Escalated ──────────────▶ Confirmed list
                                                                      (full info + last
                                                                       location to higher
                                                                       authorities)
```

**Staged visibility (key requirement):** an incident is invisible to higher
authorities until it reaches **Confirmed**. Before that, only the station/handling
officer sees it in the *pending* queue. This prevents unverified or accidental
triggers from reaching the hierarchy. It is enforced server-side in
`IncidentWorkflowService.GetPendingForOfficerAsync()` vs `GetForHierarchyAsync()`,
not just hidden in the UI.

**Confirmation rule** (configurable, in `IncidentWorkflowService.Evaluate`):
- **Confirmed** when at least one *family* contact (mother/father/guardian/sibling)
  **and** one more contact confirm the person is missing.
- **False alarm** when two or more contacts say it was a mistake.
- Otherwise the incident waits for more responses.

**Cancellation:** the victim can cancel while the incident is still `Raised` or
`UnderVerification` (mistaken trigger). Once `Confirmed`, it can no longer be
self-cancelled.

---

## 2. Victim app

| Screen | Purpose |
|---|---|
| Register | Name, mobile, **address**, **state + district** (Telangana / Andhra Pradesh), and **parent/guardian name + phone**. Creates the profile. |
| Verify | **OTP sent to the parent/guardian phone**; on success a Summit token is issued. |
| Contacts | Register mother/siblings or 3 friends **with phone numbers**. These are who the system auto-calls to verify an alert. |
| Voice enrolment | Record the distress phrase 3× to build a voiceprint. |
| Home / SOS | Big panic button **and** voice trigger; sends audio + GPS. |
| Status | Live incident status; cancel button during the grace window. |

Permissions requested: **microphone** (record distress voice) and **location**
(last-known position). Both must be requested with clear, purpose-specific consent
screens; location should use foreground + (where justified and permitted)
background access so a position is available at trigger time.

Backend endpoints (prototype):
`POST /api/app/victim/register`, `POST /api/app/victim/{id}/verify`,
`POST /api/app/victim/{id}/contacts`, `POST /api/app/victim/{id}/voice-samples`,
`POST /api/app/victim/{id}/sos`, `POST /api/app/incident/{id}/cancel`,
`GET /api/app/victim/{id}/status`.

---

## 3. Police app

| Screen | Purpose |
|---|---|
| Register / login | Police ID, **state → district → zone**, and station. Officer signs in and sees only their jurisdiction. Account is inactive until approved. |
| Pending checks | Incidents under the initial check (station officer only). |
| (auto) | Verification is automatic — the **system** calls the contacts; officers do not dial. |
| Active cases | Auto-verified incidents with full victim info + last location; officer acknowledges & dispatches. |

- **Until the initial check is done, records are not created/escalated and the
  higher authorities cannot see them** — enforced by the status filter.
- **After confirmation**, the FIR is auto-drafted and the full record (identity,
  contacts, last location) plus an escalation notification is pushed to the
  hierarchy so they can act.

Backend endpoints (prototype):
`POST /api/app/police/register`,
`GET /api/app/police/pending` (policy: ManageCases),
`POST /api/app/police/incident/{id}/verify` (policy: ManageCases),
`GET /api/app/police/confirmed` (policy: ViewCases),
`POST /api/app/ivr/callback` (telephony webhook).

---

## 3a. Parent / concerned-person app

The concerned people need visibility, not just notifications.

| Screen | Purpose |
|---|---|
| Link | Register **after** the victim, using a number she nominated (guardian or a listed contact). Strangers can't attach to a profile. |
| Live location | View her shared live location and whether she's on her expected route. Requires her **explicit opt-in** (guardian consent if a minor). |
| Raise a request | Any concerned person can raise a request. When **all** the concerned people raise one, the system creates an incident and routes it to police automatically. |
| What's happening | Live status of any active incident, so parents can follow the police response. |

**Collective-concern trigger.** Each concerned person's request is recorded
(`ConcernRequest`, deduped per phone). When the distinct count reaches the
threshold (the number of nominated contacts), `IncidentWorkflowService.RaiseConcernAsync`
creates a `DistressIncident` at `UnderVerification` with the last known location and
sends it to the station queue — a contact-initiated path alongside the victim's own
voice/panic trigger.

**Live location & route deviation.** The victim app posts periodic `LocationPing`s
(only while she has opted in). Route matching is done client-side against a chosen
route; when she deviates, `OnRoute` is false and parents are prompted to raise a
request. Continuous location is sensitive personal data — it must be consensual,
minimised, retained briefly, and (on iOS) is subject to the same background-access
limits as the microphone. Endpoints: `POST /api/app/victim/{id}/location`,
`POST /api/app/parent/register`, `GET /api/app/parent/{victimId}/location`,
`POST /api/app/parent/{victimId}/concern`.

## 4. The hard integrations (what needs a licensed provider) (what needs a licensed provider)

These are isolated behind interfaces in `Services/Safety/Integrations.cs`. The
included `Mock*` implementations let the whole flow run in development; **replace
them before any real use.**

### 4.1 Identity verification — `ISummitVerifier`
Identity verification is isolated behind a single interface. The current
implementation is a **mock** that issues an opaque Summit verification token
(`SVT-…`) and marks the profile verified — it collects no national ID number.
This keeps onboarding simple and avoids the licensing and app-store scrutiny that
come with government-ID systems. When you want stronger assurance later, slot a
real verifier behind the same interface (for example phone-OTP, a document/KYC
provider, or — if you ever pursue it — a licensed Aadhaar AUA/KUA), with no other
code changes. Whatever the source, store only the opaque token, never a raw ID.

### 4.2 Verification calls / IVR — `IVerificationCaller`
The automated "call the parents and friends" step needs a telephony provider with
programmable voice + IVR (e.g. **Exotel**, **Knowlarity**, or **Twilio**). The flow:
place a call, play a message ("Your daughter/sibling/friend X may be in danger —
press 1 if she is missing, 2 if this is a mistake"), capture the keypress, and POST
the result to `/api/app/ivr/callback`. Secure the webhook with the provider's
signature. A human officer can also record the same decision from the app, so the
system works even if a contact does not answer the IVR.

### 4.3 Voice trigger (no identity recognition)
The distress phrase only **triggers** the alert (on-device keyword spotting); the
system does **not** try to recognise or verify the speaker's identity from their
voice. Verification is done by the automated calls to the registered contacts. The
optional `IVoiceMatchService` below is left as a pluggable hook but is not on the
critical path.

#### (optional) `IVoiceMatchService`
Two separable problems: (a) **trigger** — recognising the distress phrase/pattern,
best done with an on-device keyword/wake-word model plus a manual panic button as a
guaranteed fallback; (b) **identity** — confirming the voice is the registered
person, via a speaker-verification service. Voice matching is **advisory only** in
this design: a low score never suppresses an alert — humans verify. Be honest about
false positives/negatives; never gate someone's safety on a model's confidence.

### 4.4 FIR / police records — auto-draft, not auto-file
Legally an FIR is registered by police. The workflow therefore creates a **pre-filled
draft** (`CaseType.MissingPerson`, marked `FIR-AUTO-…`) on confirmation and escalates
it for an officer to authorise. For real integration with Indian police records,
target **CCTNS** through the authorised state channel rather than writing FIRs
directly.

---

### 4.5 Hands-free triggering (voice with the app closed)
The goal: the victim says a distress phrase and the alert is sent **without opening
the app**. What's actually possible is platform-dependent, and a safety app must
not over-promise here.

- **Android — feasible, with caveats.** Run an on-device wake-word engine (e.g.
  Picovoice Porcupine, Vosk) inside a **foreground service** of type `microphone`
  with `RECORD_AUDIO`, showing a persistent "listening" notification. Key OS rule:
  a microphone foreground service **cannot be started while the app is in the
  background** — so the user opens the app once and taps "Arm protection"; the
  service then keeps listening after they leave the app or lock the screen. After a
  **reboot** the user must reopen the app to re-arm, some OEMs (Xiaomi/Oppo/etc.)
  kill background services, and it uses battery. Detection must be **on-device**
  only — never stream live audio to a server.
- **iOS — not as an always-listening app.** iOS does not give third-party apps the
  open background-microphone access needed for custom always-on wake words. Use a
  **Siri shortcut / App Intent** ("Hey Siri, I need help") or the OS-level
  **Emergency SOS** instead.
- **Always provide non-voice triggers** that also work with the app closed, because
  voice fails in noise or panic: a **hardware-button pattern** (e.g. power ×3), a
  lock-screen widget/quick-tile, a **smartwatch** SOS, or a **Bluetooth panic
  button**.

**Trigger → response flow:** distress phrase detected (no voice-identity matching) →
the **system automatically places IVR calls** to the registered contacts → on their
confirmation the case is auto-confirmed, the FIR auto-drafts, and only then is it
forwarded to police, who acknowledge and dispatch. Officers never dial contacts
themselves. (The same on-device listener can also be armed/disarmed and must respect
the cancel grace window for accidental triggers.)

## 5. Privacy, security & legal

This app handles some of the most sensitive data that exists: identity, biometrics,
precise location, family contacts, and possibly minors.

- **DPDP Act 2023 (India):** lawful basis + clear consent for each purpose; data
  minimisation; the right to erasure; breach notification. For **minors**, verifiable
  guardian consent is required.
- **Verification token:** store only the opaque token; if a real ID provider is ever added, never persist raw ID numbers.
- **Encryption:** TLS in transit; encrypt voice files and location history at rest;
  store secrets in a vault (not `appsettings.json`).
- **Access control & audit:** every read of a victim/incident is already audit-logged
  server-side; extend retention controls per policy.
- **Retention:** define and enforce retention for voice samples, trigger recordings,
  and location — keep only as long as needed.
- **Consent for contacts:** the people you list as emergency contacts are themselves
  data subjects; the consent call exists partly to inform them.

---

## 6. Reliability & safety caveats

- Provide a **manual panic button** alongside voice — voice triggers fail in noise,
  panic, or when the phone can't hear clearly.
- Handle **offline / poor network**: queue the SOS and retry; show clear delivery
  state to the victim.
- Consider **battery and background limits** on Android/iOS — a backgrounded app may
  be killed; document this honestly to users.
- Design for **false positives** (accidental triggers) with the cancel window and the
  human initial check, and for **false negatives** (don't over-trust the model).
- This must **complement, not replace** existing emergency services (e.g. 112 in
  India).

---

## 7. What's implemented in this repo

- ✅ Full backend workflow: profiles, contacts, voice-sample enrolment, distress
  incidents, staged verification, the confirmation rule, auto-draft FIR, and
  escalation — wired into the existing `ApplicationDbContext` and DI.
- ✅ REST APIs for both apps, including the IVR callback and staged-visibility
  queries.
- ✅ Integration **interfaces** with development mocks so the flow runs end to end.
- ✅ A mobile **starter** (`/mobile`) showing the victim SOS flow and the police
  verification flow against these APIs.

## 8. Not implemented (needs providers / decisions)

- ⛔ A real identity verifier (phone-OTP / KYC), real telephony/IVR, real voice models, CCTNS filing.
- ⛔ Production auth on the mobile endpoints (OTP issue + JWT; the victim onboarding
  routes are anonymous in the prototype).
- ⛔ Push notifications (FCM/APNs), background location/audio services, and the full
  multi-screen app UI.

---

## 9. Suggested next steps

1. Pick the mobile stack (the starter is React Native + Expo; Flutter or .NET MAUI
   are fine too) and let me scaffold the complete app.
2. Choose providers for identity verification, telephony, and voice; I'll implement the real
   client behind each interface.
3. Add OTP-based auth + JWT for the app endpoints.
4. Run an EF Core migration for the new tables and review the cascade rules.
