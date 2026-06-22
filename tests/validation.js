// Validation rules mirror — keep in sync with mobile *_/src/lib/validate.ts and
// src/Summit.VMS/Validation/PhoneValidation.cs. Run: node tests/validation.test.js

function normalizeMobile(raw) {
  let d = String(raw || "").replace(/\D/g, "");
  if (d.length === 12 && d.startsWith("91")) d = d.slice(2);
  else if (d.length === 11 && d.startsWith("0")) d = d.slice(1);
  return d;
}
const isMobile = (v) => /^[6-9]\d{9}$/.test(normalizeMobile(v));
const isOtp = (v) => /^\d{6}$/.test(String(v || "").trim());

const NAME_RE = /^[\p{L}][\p{L}\s.'-]{1,59}$/u;
const isName = (v) => {
  const t = String(v || "").trim();
  return t.length >= 2 && t.length <= 60 && NAME_RE.test(t);
};

module.exports = { normalizeMobile, isMobile, isOtp, isName };
