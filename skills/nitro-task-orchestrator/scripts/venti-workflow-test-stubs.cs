
// Appended to the actual workflow by test-venti-workflow.sh; no live agents or tracker writes.
enum Specified { Clear }
enum Size { Medium }
enum Risk { Medium, High }
class VentiToolException(string message) : Exception(message);
static class Venti
{
    static readonly string Scenario = Environment.GetEnvironmentVariable("WAVE_TEST_SCENARIO")!;
    static readonly Queue<object> Answers = BuildAnswers();
    static readonly List<string> Roles = [];
    static readonly List<string> Continuations = [];
    static readonly HashSet<string> Sessions = [];
    static string? LastVerification;
    static string? LastChange;

    static string Json<T>(T value) => JsonSerializer.Serialize(value, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    static readonly string Expected = Scenario switch
    {
        "dismissed" => "pass-after-verify",
        "contradictory" => "review-inconclusive",
        "missing-ruling" or "empty-plan" => "verify-inconclusive",
        "cap" => "cap-reached",
        "blocked" => "blocked",
        "agent-failed" => "agent-failed",
        _ => "pass"
    };

    public static T Args<T>(string name)
    {
        object? value = name switch
        {
            "actor" => "test-actor", "repo" => "/test/repo", "branch" => "test-branch", "area" => "parser",
            "tickets" => Scenario switch
            {
                "agent-failed" => [NewTicket("first"), NewTicket("must-not-start")],
                "five-tickets" => Enumerable.Range(1, 5).Select(i => NewTicket($"ticket-{i}")).ToArray(),
                "thirteen-tickets" => Enumerable.Range(1, 13).Select(i => NewTicket($"ticket-{i}")).ToArray(),
                _ => new[] { NewTicket("first") }
            },
            "maxCycles" => Scenario switch
            {
                "four-cycles" => 4,
                "zero-cycles" => 0,
                _ => null
            },
            _ => null
        };
        return value is null ? default! : (T)value;
    }

    static Ticket NewTicket(string id) => new(id, Scenario is "resume" or "resume-fix" ? "review" : "implement",
        Scenario is "resume" or "resume-fix" ? ["existing"] : [], Scenario is "resume" or "resume-fix" ? 1 : 0, null);

    public static string Role(string name, string does, Specified specified, Size size, Risk risk) => name;
    public static IDisposable Phase(string name) => new PhaseScope();
    public static void Log(string message) => Console.WriteLine(message);
    public static async Task<(T Value, Conversation Conversation)> AgentSession<T>(string prompt, string role, bool fork, string title)
    {
        if (role is not ("implementer" or "reviewer") || !Sessions.Add($"{role}:{TicketId(prompt)}"))
            throw new Exception("Started a duplicate session or a verifier session.");
        return (await Answer<T>(prompt, role, fork), new Conversation(role, TicketId(prompt)));
    }

    public static Task<T> Agent<T>(string prompt, string role, bool fork, string title)
    {
        if (role != "verifier") throw new Exception("Only verifiers should be one-shot agents.");
        return Answer<T>(prompt, role, fork);
    }

    static string TicketId(string prompt) => prompt.Split("Ticket: ")[1].Split('.')[0];

    public static Task<T> Continue<T>(string prompt, string role, string ticket)
    {
        if (TicketId(prompt) != ticket) throw new Exception("Session reused across tickets.");
        Continuations.Add(role);
        return Answer<T>(prompt, role, false, continuation: true);
    }

    static Task<T> Answer<T>(string prompt, string role, bool fork, bool continuation = false)
    {
        if (continuation && role == "reviewer")
        {
            if (LastVerification is null || LastChange is null ||
                !prompt.Contains(LastVerification) || !prompt.Contains(LastChange) ||
                !prompt.Contains("new evidence contradicting") || prompt.Contains("Binding rules:"))
                throw new Exception("Re-review omitted the verifier/correction handoff or repeated the initial briefing.");
        }
        if (fork || !prompt.Contains("notVerified") ||
            (!(continuation && role == "reviewer") && !prompt.Contains("--actor test-actor")))
            throw new Exception("Agent prompt omitted binding rules or fork isolation.");
        if (!continuation) Roles.Add(role);
        var next = Answers.Dequeue();
        if (next is VentiToolException error) throw error;
        if (next is VerificationResult verification) LastVerification = Json(verification);
        if (next is ChangeResult change) LastChange = Json(change);
        return Task.FromResult((T)next);
    }

    public static void Complete(WaveResult result)
    {
        if (result.Tickets.Length == 0) return;
        if (Scenario == "five-tickets")
        {
            if (result.Tickets.Any(t => t.Outcome != "pass") ||
                Answers.Count != (5 - result.Tickets.Length) * 2 ||
                Roles.Count != result.Tickets.Length * 2)
                throw new Exception("Five-ticket wave did not run each ticket sequentially.");
            if (result.Tickets.Length == 5) Console.WriteLine("PASS five-tickets");
            return;
        }
        var ticket = result.Tickets.Single();
        if (ticket.Outcome != Expected || Answers.Count != 0)
            throw new Exception($"{Scenario}: expected {Expected} with no unused answers, got {ticket.Outcome} / {Answers.Count}.");
        if (Scenario == "fix" && (!ticket.Commits.SequenceEqual(new[] { "impl", "fix" }) || ticket.Cycles != 2 ||
            ticket.Changes.Count != 2 || !ticket.NotVerified.Contains("live check") || !ticket.Deferred.Contains("NEEDS-PLANNER: note")))
            throw new Exception("Fix loop lost commits, evidence or deferred decisions.");
        if (Scenario == "resume" && (Roles.Contains("implementer") || ticket.Cycles != 2 || ticket.Commits.Single() != "existing"))
            throw new Exception("Review-only recovery replayed implementation or lost cycle state.");
        if (Scenario == "cap" && (ticket.Cycles != 3 ||
            Roles.Count(r => r == "reviewer") != 1 || Roles.Count(r => r == "implementer") != 1 ||
            Roles.Count(r => r == "verifier") != 2 ||
            Continuations.Count(r => r == "reviewer") != 2 || Continuations.Count(r => r == "implementer") != 2 ||
            ticket.Escalation is null || !ticket.Escalation.Contains("planner")))
            throw new Exception("Third failed review did not cap and escalate before another correction.");
        if (Scenario == "agent-failed" && (Roles.Count != 1 || ticket.NotVerified.Count == 0))
            throw new Exception("Agent failure did not stop subsequent writers or preserve uncertainty.");
        if ((Scenario is "fix" or "resume-fix") &&
            (!Roles.SequenceEqual(Scenario == "fix" ? new[] { "implementer", "reviewer", "verifier" } : new[] { "reviewer", "verifier", "implementer" }) ||
             !Continuations.SequenceEqual(Scenario == "fix" ? new[] { "implementer", "reviewer" } : new[] { "reviewer" })))
            throw new Exception("Correction did not reuse the implementer and reviewer sessions.");
        Console.WriteLine($"PASS {Scenario}");
    }

    static Queue<object> BuildAnswers()
    {
        var pass = Review("pass", []);
        var failing = Review("fail", [new("F1", "major", "bug", "file:1", "fix")]);
        var confirmed = Verify([new("F1", "confirmed", "test fails", "fix")], ["fix"]);
        if (Scenario == "fix")
        {
            failing = Review("fail", [.. failing.Findings, new("F2", "major", "suspected regression", "file:2", "change behavior")]);
            confirmed = Verify([.. confirmed.Findings, new("F2", "dismissed", "Existing regression test proves intended behavior", null)], ["fix"]);
        }
        object[] tail = Scenario switch
        {
            "fix" or "resume-fix" => [failing, confirmed, Change("fix"), pass],
            "dismissed" => [failing, Verify([new("F1", "dismissed", "test passes", null)], [])],
            "contradictory" => [Review("pass", failing.Findings)],
            "missing-ruling" => [failing, Verify([], ["fix"])],
            "empty-plan" => [failing, Verify(confirmed.Findings, [])],
            "cap" => [failing, confirmed, Change("fix"), failing, confirmed, Change("fix2"), failing],
            "blocked" => [],
            "agent-failed" => [],
            _ => [pass]
        };
        var queue = new Queue<object>();
        if (Scenario == "five-tickets")
        {
            for (var i = 1; i <= 5; i++)
            {
                queue.Enqueue(Change($"impl-{i}"));
                queue.Enqueue(pass);
            }
            return queue;
        }
        if (Scenario is not ("resume" or "resume-fix")) queue.Enqueue(Scenario switch
        {
            "blocked" => new ChangeResult("blocked", [], [], ["blocked checks"], "planner needed"),
            "agent-failed" => (object)new VentiToolException("agent lost"),
            _ => Change("impl")
        });
        foreach (var answer in tail) queue.Enqueue(answer);
        return queue;
    }

    static ChangeResult Change(string commit) => new("completed", [commit], ["focused tests pass"], ["live check"], "NEEDS-PLANNER: note");
    static ReviewResult Review(string verdict, Finding[] findings) => new("completed", verdict, findings, ["diff inspected"], [], null);
    static VerificationResult Verify(VerifiedFinding[] findings, string[] plan) => new("completed", findings, plan, ["finding tested"], [], null);
    sealed class PhaseScope : IDisposable { public void Dispose() { } }
}

class Conversation(string role, string ticket)
{
    public Task<T> ContinueAsync<T>(string prompt) => Venti.Continue<T>(prompt, role, ticket);
}
