// Shared input validation — mirrors the backend rules in
// src/Summit.VMS/Validation/PhoneValidation.cs so client and server agree.

export function normalizeMobile(raw: string): string {
  let d = (raw || "").replace(/\D/g, "");
  if (d.length === 12 && d.startsWith("91")) d = d.slice(2);
  else if (d.length === 11 && d.startsWith("0")) d = d.slice(1);
  return d;
}

const NAME_RE = /^[\p{L}][\p{L}\s.'-]{1,59}$/u;

export const isMobile = (v: string) => /^[6-9]\d{9}$/.test(normalizeMobile(v));
export const isOtp = (v: string) => /^\d{6}$/.test((v || "").trim());
export const isRequired = (v: string) => !!(v && v.trim());

// Each returns an error string, or null when valid — handy for inline messages.
export const requiredError = (v: string) => (isRequired(v) ? null : "Required");

export const mobileError = (v: string) =>
  !isRequired(v) ? "Required" : !isMobile(v) ? "Enter a valid 10-digit mobile (starts 6-9)" : null;

export const otpError = (v: string) =>
  !isRequired(v) ? "Required" : !isOtp(v) ? "Enter the 6-digit code" : null;

export const nameError = (v: string) => {
  const t = (v || "").trim();
  if (!t) return "Required";
  if (t.length < 2) return "At least 2 characters";
  if (t.length > 60) return "At most 60 characters";
  return NAME_RE.test(t) ? null : "Use letters, spaces, . ' - only";
};

export const textError = (v: string, min: number, max: number) => {
  const t = (v || "").trim();
  if (!t) return "Required";
  if (t.length < min) return `At least ${min} characters`;
  if (t.length > max) return `At most ${max} characters`;
  return null;
};
