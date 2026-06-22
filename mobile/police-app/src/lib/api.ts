// Police app — API client. Authenticated with the JWT from /api/auth/login.
export const API_BASE = "https://10.0.2.2:5001";

let token: string | null = null;
export function setToken(t: string) { token = t; }

function authHeaders(json = true): Record<string, string> {
  const h: Record<string, string> = {};
  if (json) h["Content-Type"] = "application/json";
  if (token) h["Authorization"] = `Bearer ${token}`;
  return h;
}

async function jsonOf<T>(res: Response): Promise<T> {
  if (!res.ok) throw new Error(`${res.status} ${await res.text()}`);
  return res.json() as Promise<T>;
}

export interface PendingIncident {
  id: number; victimName: string; victimMobile: string; status: string;
  latitude?: number; longitude?: number; raisedAtUtc: string;
  contacts: { contactId: number; name: string; relation: string; phone: string; decision: string }[];
}
export interface ConfirmedIncident {
  id: number; victimName: string; victimMobile: string; status: string;
  latitude?: number; longitude?: number; confirmedAtUtc?: string; caseId?: number;
}

export const PoliceApi = {
  login: (email: string, password: string) =>
    fetch(`${API_BASE}/api/auth/login`, {
      method: "POST", headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ email, password }),
    }).then((r) => jsonOf<{ accessToken: string }>(r)),

  register: (body: Record<string, unknown>) =>
    fetch(`${API_BASE}/api/app/police/register`, {
      method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(body),
    }).then((r) => jsonOf(r)),

  pending: () =>
    fetch(`${API_BASE}/api/app/police/pending`, { headers: authHeaders(false) })
      .then((r) => jsonOf<PendingIncident[]>(r)),

  verify: (incidentId: number, contactId: number, decision: string) =>
    fetch(`${API_BASE}/api/app/police/incident/${incidentId}/verify`, {
      method: "POST", headers: authHeaders(),
      body: JSON.stringify({ contactId, decision }),
    }).then((r) => jsonOf(r)),

  confirmed: () =>
    fetch(`${API_BASE}/api/app/police/confirmed`, { headers: authHeaders(false) })
      .then((r) => jsonOf<ConfirmedIncident[]>(r)),
};
