// Run: node tests/validation.test.js
const V = require("./validation");

let passed = 0, failed = 0;
function ok(cond, name) {
  console.log(`${cond ? "  ok  " : " FAIL "} ${name}`);
  cond ? passed++ : failed++;
}

console.log("\nMobile normalization");
ok(V.normalizeMobile("98765 43210") === "9876543210", "strips spaces");
ok(V.normalizeMobile("+91 98765 43210") === "9876543210", "strips +91");
ok(V.normalizeMobile("098765-43210") === "9876543210", "strips leading 0 and dashes");
ok(V.normalizeMobile("(98765)43210") === "9876543210", "strips brackets");

console.log("\nMobile validity (Indian 10-digit, starts 6-9)");
ok(V.isMobile("9876543210") === true, "valid 9-start");
ok(V.isMobile("6123456789") === true, "valid 6-start");
ok(V.isMobile("+919876543210") === true, "valid with country code");
ok(V.isMobile("5123456789") === false, "rejects 5-start");
ok(V.isMobile("12345") === false, "rejects too short");
ok(V.isMobile("98765432101") === false, "rejects too long");
ok(V.isMobile("98765abcde") === false, "rejects letters");
ok(V.isMobile("") === false, "rejects empty");

console.log("\nOTP validity (6 numeric digits)");
ok(V.isOtp("123456") === true, "valid 6-digit");
ok(V.isOtp("12345") === false, "rejects 5-digit");
ok(V.isOtp("1234567") === false, "rejects 7-digit");
ok(V.isOtp("12a456") === false, "rejects letters");
ok(V.isOtp("") === false, "rejects empty");

console.log("\nName validity (2-60, letters/spaces/.'- only)");
ok(V.isName("Ravi") === true, "simple name");
ok(V.isName("Ravi Kumar") === true, "two words");
ok(V.isName("K. Ravi") === true, "initial with dot");
ok(V.isName("O'Brien") === true, "apostrophe");
ok(V.isName("A") === false, "rejects single char");
ok(V.isName("Ravi123") === false, "rejects digits");
ok(V.isName("R@vi") === false, "rejects symbols");
ok(V.isName("   ") === false, "rejects blank");
ok(V.isName("x".repeat(61)) === false, "rejects over 60 chars");

console.log(`\n${passed} passed, ${failed} failed\n`);
process.exit(failed ? 1 : 0);
