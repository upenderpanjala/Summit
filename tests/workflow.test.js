// Run: node tests/workflow.test.js
const W = require("./workflow");
const { Decision: D, Status: S } = W;

let passed = 0, failed = 0;
function eq(actual, expected, name) {
  const ok = actual === expected;
  console.log(`${ok ? "  ok  " : " FAIL "} ${name}` + (ok ? "" : `  (got ${actual}, expected ${expected})`));
  ok ? passed++ : failed++;
}
function c(family, decision) { return { family, decision }; }

console.log("\nConfirmation rule (>=1 family AND >=2 total confirm)");
eq(W.evaluateConfirmation([c(true, D.ConfirmedMissing), c(true, D.ConfirmedMissing), c(false, D.Pending)]),
   S.Confirmed, "mother + father confirm -> Confirmed");
eq(W.evaluateConfirmation([c(true, D.ConfirmedMissing), c(false, D.ConfirmedMissing), c(false, D.Pending)]),
   S.Confirmed, "mother + friend confirm -> Confirmed");
eq(W.evaluateConfirmation([c(false, D.ConfirmedMissing), c(false, D.ConfirmedMissing), c(false, D.Pending)]),
   S.UnderVerification, "two friends, no family -> stays UnderVerification");
eq(W.evaluateConfirmation([c(true, D.ConfirmedMissing), c(false, D.Pending), c(false, D.Pending)]),
   S.UnderVerification, "single family confirm -> stays UnderVerification");
eq(W.evaluateConfirmation([c(true, D.ConfirmedMissing), c(true, D.ConfirmedMissing), c(false, D.ConfirmedMissing)]),
   S.Confirmed, "all three confirm -> Confirmed");

console.log("\nFalse alarm (>=2 deny)");
eq(W.evaluateConfirmation([c(true, D.DeniedFalseAlarm), c(false, D.DeniedFalseAlarm), c(false, D.Pending)]),
   S.FalseAlarm, "two denials -> FalseAlarm");
eq(W.evaluateConfirmation([c(true, D.DeniedFalseAlarm), c(false, D.Pending), c(false, D.Pending)]),
   S.UnderVerification, "single denial -> stays UnderVerification");
eq(W.evaluateConfirmation([c(true, D.ConfirmedMissing), c(true, D.ConfirmedMissing), c(false, D.DeniedFalseAlarm)]),
   S.Confirmed, "2 family confirm beats 1 denial -> Confirmed");

console.log("\nCollective-concern threshold (= contact count, min 2)");
eq(W.concernThreshold(3), 3, "3 contacts -> threshold 3");
eq(W.concernThreshold(0), 2, "0 contacts -> threshold 2 (floor)");
eq(W.shouldEscalateConcern(3, 3), true, "3 of 3 concerned -> escalate");
eq(W.shouldEscalateConcern(2, 3), false, "2 of 3 concerned -> do not escalate");

console.log("\nStaged visibility to higher authorities");
eq(W.visibleToHierarchy(S.Raised), false, "Raised hidden");
eq(W.visibleToHierarchy(S.UnderVerification), false, "UnderVerification hidden");
eq(W.visibleToHierarchy(S.Confirmed), true, "Confirmed visible");
eq(W.visibleToHierarchy(S.Escalated), true, "Escalated visible");

console.log("\nSelf-cancel window");
eq(W.canCancel(S.UnderVerification), true, "cancel allowed while verifying");
eq(W.canCancel(S.Confirmed), false, "cancel blocked once confirmed");
eq(W.canCancel(S.Escalated), false, "cancel blocked once escalated");

console.log(`\n${passed} passed, ${failed} failed\n`);
process.exit(failed ? 1 : 0);
