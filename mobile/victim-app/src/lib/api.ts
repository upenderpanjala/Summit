// Victim app — API client for the Summit Safety backend.
// Set API_BASE to your backend (use your machine's LAN IP on a real device).
export const API_BASE = "https://10.0.2.2:5001"; // Android emulator -> host machine

export type RegistrationStatus =
  | "PendingVerification" | "PendingContactConsent" | "PendingVoiceEnrollment"
  | "Active" | "Suspended";

export interface ProfileState {
  id: number;
  fullName: string;
  registrationStatus: RegistrationStatus;
  verificationStatus: string;
  contactCount: number;
  voiceSampleCount: number;
  activeIncidentStatus: string | null;
}

async function json<T>(res: Response): Promise<T> {
  if (!res.ok) throw new Error(`${res.status} ${await res.text()}`);
  return res.json() as Promise<T>;
}

export const VictimApi = {
  register: (body: Record<string, unknown>) =>
    fetch(`${API_BASE}/api/app/victim/register`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body),
    }).then((r) => json<ProfileState>(r)),

  // Send an OTP to the parent/guardian phone
  sendOtp: (id: number) =>
    fetch(`${API_BASE}/api/app/victim/${id}/otp/send`, { method: "POST" }).then((r) => json(r)),

  // Verify the OTP -> issues the Summit token and marks the profile verified
  verifyOtp: (id: number, code: string) =>
    fetch(`${API_BASE}/api/app/victim/${id}/otp/verify`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ code }),
    }).then((r) => json(r)),

  addContacts: (id: number, contacts: unknown[]) =>
    fetch(`${API_BASE}/api/app/victim/${id}/contacts`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(contacts),
    }).then((r) => json(r)),

  // audioUri comes from expo-av recording; ordinal 1..3
  enrollVoice: (id: number, audioUri: string, ordinal: number) => {
    const form = new FormData();
    form.append("ordinal", String(ordinal));
    form.append("audio", { uri: audioUri, name: `sample${ordinal}.m4a`, type: "audio/m4a" } as any);
    return fetch(`${API_BASE}/api/app/victim/${id}/voice-samples`, { method: "POST", body: form })
      .then((r) => json(r));
  },

  sos: (id: number, audioUri: string | null, lat?: number, lng?: number) => {
    const form = new FormData();
    if (lat != null) form.append("lat", String(lat));
    if (lng != null) form.append("lng", String(lng));
    if (audioUri) form.append("audio", { uri: audioUri, name: "trigger.m4a", type: "audio/m4a" } as any);
    return fetch(`${API_BASE}/api/app/victim/${id}/sos`, { method: "POST", body: form })
      .then((r) => json(r));
  },

  cancel: (incidentId: number) =>
    fetch(`${API_BASE}/api/app/incident/${incidentId}/cancel`, { method: "POST" }).then((r) => json(r)),

  status: (id: number) =>
    fetch(`${API_BASE}/api/app/victim/${id}/status`).then((r) => json<ProfileState>(r)),
};
