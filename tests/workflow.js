// Workflow decision rules — a pure-JS mirror of
// src/Summit.VMS/Services/Safety/IncidentWorkflowService.cs, so the core logic
// can be unit-tested in any environment (run: `node tests/workflow.test.js`).

const Decision = {
  Pending: "Pending",
  ConfirmedMissing: "ConfirmedMissing",
  DeniedFalseAlarm: "DeniedFalseAlarm",
  NoResponse: "NoResponse",
};

const Status = {
  Raised: "Raised",
  UnderVerification: "UnderVerification",
  Confirmed: "Confirmed",
  FirRegistered: "FirRegistered",
  Escalated: "Escalated",
  Resolved: "Resolved",
  FalseAlarm: "FalseAlarm",
  Cancelled: "Cancelled",
};

// Confirmed when >=1 family AND >=2 total confirm; FalseAlarm when >=2 deny.
function evaluateConfirmation(contacts) {
  let family = 0, other = 0, denials = 0;
  for (const c of contacts) {
    if (c.decision === Decision.ConfirmedMissing) c.family ? family++ : other++;
    else if (c.decision === Decision.DeniedFalseAlarm) denials++;
  }
  if (family >= 1 && family + other >= 2) return Status.Confirmed;
  if (denials >= 2) return Status.FalseAlarm;
  return Status.UnderVerification;
}

// "All the concerned people" — default to the number of nominated contacts (min 2).
function concernThreshold(contactCount) {
  return Math.max(2, contactCount);
}
function shouldEscalateConcern(distinctConcernCount, contactCount) {
  return distinctConcernCount >= concernThreshold(contactCount);
}

// Higher authorities only see verified+ incidents (staged visibility).
function visibleToHierarchy(status) {
  return [Status.Confirmed, Status.FirRegistered, Status.Escalated, Status.Resolved].includes(status);
}

// Self-cancel allowed only before confirmation.
function canCancel(status) {
  return status === Status.Raised || status === Status.UnderVerification;
}

module.exports = {
  Decision, Status,
  evaluateConfirmation, concernThreshold, shouldEscalateConcern,
  visibleToHierarchy, canCancel,
};
