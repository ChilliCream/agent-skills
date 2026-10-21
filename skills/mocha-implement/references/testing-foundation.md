# Testing foundation

## Goal

Write a messaging test that fails for a real regression and never for a timing accident.

## Agent enforcement rules

### Do

- Start a real `IHost` with the InMemory transport. `AddMessageBus()` registers a hosted service, so starting the host
  is what starts the runtime, the transports and the background workers.
- Resolve `IMessageBus` from a scope. It is a scoped service.
- Wait on a signal that the handler raises, with a generous timeout.
- Prefer the response when the flow is request/reply. It needs no synchronization at all.
- Use `FakeTimeProvider` for anything involving time.

### Do not

- Do not synchronize with `Task.Delay`. A sleep followed by an assertion is a race condition with a timer attached, and
  a polling loop is a sleep with extra steps.
- Do not build a service provider by hand and expect messages to flow. Nothing is started.
- Do not assert mid-flight saga state. Assert the messages it sent and the final outcome.
- Do not use `Assert.DoesNotContain`. It proves something is absent without verifying what is present.
- Do not reach for a real broker for logic a transport does not affect.

## Pick the tier

| What you are testing | Setup |
| --- | --- |
| Handler logic, routing, patterns, saga flow | `IHost` plus InMemory transport |
| Mediator handlers and middleware | Service provider plus `ISender`, no host |
| Outbox, inbox, saga persistence | Real database plus InMemory transport |
| Broker semantics: acknowledgement, prefetch, connection recovery, topology conflicts | Real broker in a container |

Each tier up costs wall-clock time and flakiness. Only pay for it when the behavior under test actually depends on what
the tier adds.

## Build checklist

- One host per test, disposed at the end.
- A recorder singleton that the handler under test writes to, carrying the signal.
- A `WaitAsync` with a generous timeout plus an assertion message naming what failed to arrive.
- Assertions on the recorded message, the response, or the database, never on a sleep.
- A failure-path test: timeout, fault, or duplicate delivery.

## Example: the host helper

```csharp
private static async Task<IHost> StartBusAsync(
    Action<IMessageBusHostBuilder> configure,
    CancellationToken cancellationToken)
{
    var builder = Host.CreateApplicationBuilder();

    var bus = builder.Services.AddMessageBus();
    configure(bus);
    bus.AddInMemory();

    var host = builder.Build();
    await host.StartAsync(cancellationToken);

    return host;
}
```

For a service that has its own `Program.cs`, use `WebApplicationFactory` and override the transport in
`ConfigureTestServices` rather than re-describing the bus in the test.

## Example: the recorder

```csharp
public sealed class MessageRecorder
{
    private readonly SemaphoreSlim _signal = new(0);
    private readonly ConcurrentBag<object> _messages = [];

    public IReadOnlyCollection<object> Messages => _messages;

    public void Record(object message)
    {
        _messages.Add(message);
        _signal.Release();
    }

    public async Task<bool> WaitAsync(TimeSpan timeout, int expectedCount = 1)
    {
        for (var i = 0; i < expectedCount; i++)
        {
            if (!await _signal.WaitAsync(timeout))
            {
                return false;
            }
        }

        return true;
    }
}
```

```csharp
public sealed class OrderPlacedTestHandler(MessageRecorder recorder) : IEventHandler<OrderPlacedEvent>
{
    public ValueTask HandleAsync(OrderPlacedEvent message, CancellationToken cancellationToken)
    {
        recorder.Record(message);
        return default;
    }
}
```

## Example: the test

```csharp
[Fact]
public async Task PublishAsync_Should_InvokeHandler_When_EventIsPublished()
{
    // arrange
    var orderId = Guid.NewGuid();
    var recorder = new MessageRecorder();

    using var host = await StartBusAsync(
        bus =>
        {
            bus.Services.AddSingleton(recorder);
            bus.AddEventHandler<OrderPlacedTestHandler>();
        },
        TestContext.Current.CancellationToken);

    using var scope = host.Services.CreateScope();
    var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

    // act
    await bus.PublishAsync(
        new OrderPlacedEvent { OrderId = orderId, TotalAmount = 49.99m },
        TestContext.Current.CancellationToken);

    // assert
    Assert.True(
        await recorder.WaitAsync(TimeSpan.FromSeconds(10)),
        "the handler did not receive the event");

    var received = Assert.IsType<OrderPlacedEvent>(Assert.Single(recorder.Messages));
    Assert.Equal(orderId, received.OrderId);
}
```

A signal-based wait is fast on success, because it returns the moment the handler finishes, and fails with a clear
message on a real regression.

## Example: asserting a negative

```csharp
Assert.False(
    await recorder.WaitAsync(TimeSpan.FromMilliseconds(500), expectedCount: 2),
    "the duplicate message should not have been delivered");
Assert.Single(recorder.Messages);
```

"The duplicate never arrived" and "the filtered message was not delivered" are the same shape: wait briefly for a
second signal and require that it does not come, then assert what is present.

## Guardrails

- Name tests `Method_Should_Outcome_When_Condition` and keep to at most five `Assert` calls. Beyond that, snapshot the
  state instead.
- Keyed registrations keep per-handler expectations apart when several handlers consume the same event.
- Test retry and redelivery counts by counting handler invocations in the recorder, not by inspecting the policy.
- A test for an error path asserts the message reached the error endpoint, not just that the handler threw.
- Dispose the host. A leaked host keeps background workers running into the next test.
- `Mocha.Testing` is in development and not released. Until it ships, these patterns are the supported way to test.

## Default implementation shape

1. Helper that starts an `IHost` with the InMemory transport.
2. Recorder singleton plus a test handler that records.
3. One test per behavior, arranged, acted and asserted with a signal wait.
4. A failure-path test alongside each happy path.

## Verification

```bash
dotnet build
dotnet test --filter "FullyQualifiedName~Messaging"
```

Run the new test ten times before trusting it. A test that passes nine times out of ten is reporting a synchronization
bug in itself, not in the code under test.

## References

- [Hosting](https://chillicream.com/docs/mocha/hosting)
- [Transports](https://chillicream.com/docs/mocha/transports)
- [mocha-best-practices: testing](../../mocha-best-practices/references/testing.md)
- [mocha-best-practices: reliability](../../mocha-best-practices/references/reliability.md)
