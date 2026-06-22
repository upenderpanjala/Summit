// Parent / concerned-person app — API client for the Summit Safety backend.
// Set API_BASE to your backend (use your machine's LAN IP on a real device).
export const API_BASE = "https://10.0.2.2:5001"; // Android emulator -> host machine

export type ContactRelation = "Mother" | "Father" | "Guardian" | "Sibling" | "Friend" | "Other";

export interface ParentLink {
  victimProfileId: number;
  victimName: string;
  approved: boolean;
  message: string;
}

export interface LiveLocation {
  lat: number | null;
  lng: number | null;
  onRoute: boolean;
  at: string | null;
  incidentStatus: string | null;
  concernCount: number;
  concernThreshold: number;
}

export interface ConcernResult {
  concernCount: number;
  threshold: number;
  escalatedToPolice: boolean;
  incidentId: number | null;
  incidentStatus: string | null;
}

async function json<T>(res: Response): Promise<T> {
  if (!res.ok) throw new Error(`${res.status} ${await res.text()}`);
  return res.json() as Promise<T>;
}

export const ParentApi = {
  // Register after the victim has registered, using a number she nominated.
  register: (body: { victimMobile: string; name: string; relation: ContactRelation; phone: string }) =>
    fetch(`${API_BASE}/api/app/parent/register`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body),
    }).then((r) => json<ParentLink>(r)),

  // Live location + current concern/incident state.
  location: (victimId: number) =>
    fetch(`${API_BASE}/api/app/parent/${victimId}/location`).then((r) => json<LiveLocation>(r)),

  // Raise a request. When enough concerned people raise one, it escalates to police.
  concern: (victimId: number, body: { phone: string; name?: string; reason?: string }) =>
    fetch(`${API_BASE}/api/app/parent/${victimId}/concern`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(body),
    }).then((r) => json<ConcernResult>(r)),
};
