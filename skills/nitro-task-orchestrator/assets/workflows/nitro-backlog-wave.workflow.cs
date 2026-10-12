// Runs one claimed Nitro wave through implementation, independent review, adversarial verification and correction.
using System.Text.Json;

var actor = Venti.Args<string>("actor");
var repo = Venti.Args<string>("repo");
var branch = Venti.Args<string>("branch");
var area = Venti.Args<string>("area");
var tickets = Venti.Args<Ticket[]>("tickets");
var rules = Venti.Args<string?>("rules") ?? "";
var tooling = Venti.Args<string?>("tooling") ?? "";
var maxCycles = Venti.Args<int?>("maxCycles") ?? 3;
var stopOnFailure = Venti.Args<bool?>("stopOnFailure") ?? false;

if (new[] { actor, repo, branch, area }.Any(string.IsNullOrWhiteSpace) || tickets.Length == 0)
    throw new ArgumentException("actor, repo, branch, area and a nonempty tickets array are required.");

if (maxCycles is < 1 or > 3)
    throw new ArgumentException("maxCycles must be between 1 and 3.");

if (tickets.Select(t => t.Id).Distinct().Count() != tickets.Length ||
    tickets.Any(t => string.IsNullOrWhiteSpace(t.Id) || t.Mode is not ("implement" or "review") ||
        t.PriorCycles < 0 || t.PriorCycles >= maxCycles ||
        (t.Mode == "review" && t.Commits.Length == 0)))
    throw new ArgumentException("Tickets need unique ids, implement/review mode, valid priorCycles and commits for review mode.");

// Four agents per failed round, then implement/review: keep within the 50-agent run budget.
if (tickets.Length * (4 * maxCycles - 2) > 50)
    throw new ArgumentException("Split this wave into smaller runs to stay within 50 agents.");

var implementer =
    Venti.Role(
        "implementer",
        "Implements one Nitro ticket, tests and self-reviews it, then commits only its scoped files.",
        specified: Specified.Clear,
        size: Size.Medium,
        risk: Risk.Medium);

var reviewer =
    Venti.Role(
        "reviewer",
        "Independently reviews actual commits for correctness, scope creep and verification gaps without editing files.",
        specified: Specified.Clear,
        size: Size.Medium,
        risk: Risk.High);

var verifier =
    Venti.Role(
        "verifier",
        "Adversarially confirms or dismisses failed-review findings and supplies a minimal correction plan without editing files.",
        specified: Specified.Clear,
        size: Size.Medium,
        risk: Risk.High);

var fixer =
    Venti.Role(
        "fixer",
        "Applies only the verified correction plan, runs focused checks and adds scoped commits.",
        specified: Specified.Clear,
        size: Size.Medium,
        risk: Risk.Medium);

var standingRules = $"""
Binding rules:
- Work only inside repository {repo}, on existing branch {branch}, within area {area}. No other writers may run during this workflow.
- Read the ticket and its binding comments with nitro agent tasks show <id> --output json before acting. Check its labels and scope: never work on wayfinder:* or research* tasks. Report blocked if the ticket is not claimed by {actor} or does not belong to this area.
- Read repository instructions. Do not touch the user's uncommitted files.
- Atomic commits with pathspec: git commit -m "..." -- <files>. Never git add then bare git commit.
- Run the repository formatter in write then check mode on touched files before committing. Run focused verification; do not share build outputs with other processes. Restore only your own known build side effects.
- On git index.lock failure wait and retry; never force or delete the lock.
- Never close or claim tasks, push, switch branches, rebase, merge, amend or rewrite commits. Only the orchestrator changes task status.
- Pass --actor {actor} on every task comment. If a conservative interpretation is possible, record NEEDS-PLANNER: <question> | chose: <what I did> and continue; otherwise report blocked.
- Report only commands actually run and results actually seen. notVerified lists all checks or claims not exercised. escalation contains deferred decisions or a blocker, otherwise null.
Project rules from the orchestrator and shared memory:
{rules}
Tooling and environment instructions:
{tooling}
""";

string Header(Ticket ticket) => $"""
Ticket: {ticket.Id}. Repository: {repo}. Branch: {branch}. Area: {area}.
Read nitro agent tasks show {ticket.Id} --output json, including comments.
Orchestrator context (binding): {ticket.Context ?? "none"}
{standingRules}
""";

string Json<T>(T value) =>
    JsonSerializer.Serialize(
        value,
        new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

var results = new List<TicketResult>();
Venti.Complete(new WaveResult(branch, area, results.ToArray()));
foreach (var ticket in tickets)
{
    var result = new TicketResult(ticket.Id, ticket.Commits.ToList());
    results.Add(result);

    try
    {
        await RunTicket(ticket, result);
    }
    catch (VentiToolException error)
    {
        result.Outcome = "agent-failed";
        result.Escalation = error.Message;
        result.NotVerified.Add("The failed agent may have left unreported changes or commits; inspect git state before retrying.");
    }
    Venti.Log($"{area}: {ticket.Id} -> {result.Outcome} ({result.Commits.Count} commits, {result.Cycles} review cycles)");
    Venti.Complete(new WaveResult(branch, area, results.ToArray()));
    // An agent failure may have left an unknown working tree; do not send another writer into it.
    if (result.Outcome == "agent-failed" || (stopOnFailure && !result.Outcome.StartsWith("pass")))
        break;
}

async Task RunTicket(Ticket ticket, TicketResult result)
{
    void Take(string[] notVerified, string? escalation)
    {
        result.NotVerified.AddRange(notVerified);

        if (escalation is not null)
            result.Deferred.Add(escalation);
    }

    if (ticket.Mode == "implement")
    {
        ChangeResult change;

        using (Venti.Phase("Implement"))
            change =
                await Venti.Agent<ChangeResult>(
                    $"""
                    ROLE: Implementer. Implement exactly this ticket: problem, file scope, fix direction, verification and non-goals.
                    {Header(ticket)}
                    Fix the root cause, add focused tests, then self-review correctness, scope creep and verification gaps. Fix your own findings before reporting.
                    Commit with pathspec and a conventional subject. Return status (completed/blocked/failed), new commit hashes, verified, notVerified and escalation.
                    """,
                    implementer,
                    fork: false,
                    title: $"Implement {ticket.Id}");

        result.Changes.Add(change);
        Take(change.NotVerified, change.Escalation);
        result.Commits.AddRange(change.Commits);

        if (change.Status != "completed" || change.Commits.Length == 0)
        {
            result.Outcome = change.Status == "blocked" ? "blocked" : "implement-failed";
            result.Escalation = change.Escalation;
            return;
        }
    }

    for (var cycle = ticket.PriorCycles + 1; cycle <= maxCycles; cycle++)
    {
        result.Cycles = cycle;
        ReviewResult review;

        using (Venti.Phase("Review"))
            review =
                await Venti.Agent<ReviewResult>(
                    $"""
                    ROLE: Independent reviewer, cycle {cycle}. Modify nothing, commit nothing, change no task status.
                    {Header(ticket)}
                    Read actual diffs and surrounding code for commits {Json(result.Commits)}, not an implementation summary.
                    Judge correctness (root cause, no suppression), scope creep (anything beyond the ticket) and verification gaps.
                    Re-run focused checks where feasible. Check these outstanding verification gaps first: {Json(result.NotVerified)}.
                    Return status (completed/blocked/failed), verdict (pass/fail/unknown), findings with unique ids, severity (blocker/major/minor), summary, evidence (file:line or command output), remediation, verified, notVerified and escalation.
                    Pass only with zero blocker or major findings. Minor findings do not fail review.
                    """,
                    reviewer,
                    fork: false,
                    title: $"Review {ticket.Id} {cycle}");

        result.Reviews.Add(review);
        Take(review.NotVerified, review.Escalation);

        if (review.Status != "completed" || review.Verdict is not ("pass" or "fail") ||
            review.Findings.Any(f => f.Severity is not ("blocker" or "major" or "minor")) ||
            review.Findings.Select(f => f.Id).Distinct().Count() != review.Findings.Length)
        {
            result.Outcome = "review-inconclusive";
            return;
        }

        var failing =
            review.Findings
                .Where(f => f.Severity != "minor")
                .ToArray();

        if (review.Verdict == "pass" && failing.Length == 0)
        {
            result.Outcome = "pass";
            return;
        }

        if (failing.Length == 0 || review.Verdict == "pass")
        {
            result.Outcome = "review-inconclusive";
            result.Escalation = "Review verdict contradicts its findings.";
            return;
        }

        if (cycle == maxCycles)
        {
            result.Outcome = "cap-reached";
            result.Escalation = $"Review failed after {maxCycles} cycles; escalate to the planner.";
            return;
        }

        VerificationResult verification;

        using (Venti.Phase("Verify"))
            verification =
                await Venti.Agent<VerificationResult>(
                    $"""
                    ROLE: Adversarial verifier. Modify nothing, commit nothing. Try to refute every finding using code and focused checks.
                    {Header(ticket)}
                    Commits: {Json(result.Commits)}. Review: {Json(review)}.
                    Return status (completed/blocked/failed), findings keeping exactly the review ids, disposition (confirmed/dismissed/modified), evidence and correction (string or null), correctionPlan, verified, notVerified and escalation.
                    Explain every dismissal. correctionPlan is the smallest ordered set of edits resolving confirmed/modified blocker and major findings, not minor findings or unrelated refactoring.
                    """,
                    verifier,
                    fork: false,
                    title: $"Verify {ticket.Id} {cycle}");

        result.Verifications.Add(verification);
        Take(verification.NotVerified, verification.Escalation);

        if (verification.Status != "completed" ||
            verification.Findings.Any(f => f.Disposition is not ("confirmed" or "dismissed" or "modified")) ||
            !verification.Findings.Select(f => f.Id).Order().SequenceEqual(review.Findings.Select(f => f.Id).Order()))
        {
            result.Outcome = "verify-inconclusive";
            result.Escalation = "Verifier did not rule exactly once on every review finding.";
            return;
        }

        if (failing.All(f => verification.Findings.Single(v => v.Id == f.Id).Disposition == "dismissed"))
        {
            result.Outcome = "pass-after-verify";
            return;
        }

        if (verification.CorrectionPlan.Length == 0)
        {
            result.Outcome = "verify-inconclusive";
            result.Escalation = "Verifier confirmed findings without a correction plan.";
            return;
        }

        ChangeResult fix;

        using (Venti.Phase("Fix"))
            fix =
                await Venti.Agent<ChangeResult>(
                    $"""
                    ROLE: Fixer. Apply only this verified correction plan, no dismissed findings or wider scope.
                    {Header(ticket)}
                    Existing commits: {Json(result.Commits)}. Verification: {Json(verification)}.
                    Run focused checks, add new pathspec commits on top, never amend or rewrite.
                    Return status (completed/blocked/failed), new commit hashes only, verified, notVerified and escalation.
                    """,
                    fixer,
                    fork: false,
                    title: $"Fix {ticket.Id} {cycle}");

        result.Changes.Add(fix);
        Take(fix.NotVerified, fix.Escalation);
        result.Commits.AddRange(fix.Commits);

        if (fix.Status != "completed" || fix.Commits.Length == 0)
        {
            result.Outcome = "fix-failed";
            result.Escalation = fix.Escalation;
            return;
        }
    }
}

record Ticket(
    string Id,
    string Mode,
    string[] Commits,
    int PriorCycles,
    string? Context);

// These records mirror the three JSON result schemas; semantic verdicts are checked before accepting a pass.
record ChangeResult(
    string Status,
    string[] Commits,
    string[] Verified,
    string[] NotVerified,
    string? Escalation);

record Finding(
    string Id,
    string Severity,
    string Summary,
    string Evidence,
    string Remediation);

record ReviewResult(
    string Status,
    string Verdict,
    Finding[] Findings,
    string[] Verified,
    string[] NotVerified,
    string? Escalation);

record VerifiedFinding(
    string Id,
    string Disposition,
    string Evidence,
    string? Correction);

record VerificationResult(
    string Status,
    VerifiedFinding[] Findings,
    string[] CorrectionPlan,
    string[] Verified,
    string[] NotVerified,
    string? Escalation);

record WaveResult(
    string Branch,
    string Area,
    TicketResult[] Tickets);

class TicketResult(string id, List<string> commits)
{
    public string Id { get; } = id;
    public string Outcome { get; set; } = "unknown";
    public List<string> Commits { get; } = commits;
    public int Cycles { get; set; }
    public List<ChangeResult> Changes { get; } = [];
    public List<ReviewResult> Reviews { get; } = [];
    public List<VerificationResult> Verifications { get; } = [];
    public List<string> Deferred { get; } = [];
    public List<string> NotVerified { get; } = [];
    public string? Escalation { get; set; }
}