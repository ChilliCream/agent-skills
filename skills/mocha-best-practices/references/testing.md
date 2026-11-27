# Testing

Messaging tests fail in a characteristic way: they pass locally, pass in CI nineteen times, and fail on the twentieth
because the assertion ran before the handler did. Every rule here exists to remove that class of failure.

## Rules

1. **Start a real host with the InMemory transport.** `AddMessageBus()` registers a hosted service that starts the
   messaging runtime, so starting the host starts the bus, the transports and the background workers (outbox,
   scheduler, inbox cleanup). Building a service provider by hand starts none of that.

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

   For a service under test that has its own `Program.cs`, use `WebApplicationFactory` and override the transport in
   `ConfigureTestServices` instead of re-describing the bus in the test.

2. **Resolve `IMessageBus` from a scope.** It is a scoped service, so resolving it from the root provider is wrong and
   will eventually bite you on a scoped dependency.

   ```csharp
   using var scope = host.Services.CreateScope();
   var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

   await bus.PublishAsync(new OrderPlacedEvent { OrderId = orderId, /* ... */ }, cancellationToken);
   ```

   In a multi-step test, each step gets its own scope, the same way each message gets its own scope in production.

3. **Never sleep to synchronize. Wait on a signal.** `await Task.Delay(500)` followed by an assertion is both slow and
   unsound: it is a guess about timing that the CI machine is free to disagree with.

   ```csharp
   // wrong
   await bus.PublishAsync(new OrderPlacedEvent { /* ... */ }, ct);
   await Task.Delay(500, ct);
   Assert.Single(recorder.Messages);
   ```

   ```csharp
   // right
   await bus.PublishAsync(new OrderPlacedEvent { /* ... */ }, ct);
   Assert.True(await recorder.WaitAsync(TimeSpan.FromSeconds(10)), "handler did not receive the event");
   var order = Assert.IsType<OrderPlacedEvent>(Assert.Single(recorder.Messages));
   ```

   A signal-based wait with a generous timeout is fast on success (it returns the moment the handler finishes) and
   fails with a clear message on a real regression. A polling loop around a predicate is a sleep with extra steps.

4. **Build the signal into a recorder the handler writes to.** One small test-only type, registered as a singleton and
   injected into the handler under test:

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

   Keyed registrations (`[FromKeyedServices("billing")]`) keep per-handler expectations apart when several handlers
   consume the same event.

5. **Assert a negative with a short timeout, not with a sleep.** "The duplicate never arrived" and "the filtered
   message was not delivered" are the same shape: wait briefly for a second signal and require that it does not come.

   ```csharp
   Assert.False(
       await recorder.WaitAsync(TimeSpan.FromMilliseconds(500), expectedCount: 2),
       "the duplicate message should not have been delivered");
   Assert.Single(recorder.Messages);
   ```

   Pair every negative with a positive assertion about what *did* happen. A negative on its own passes when nothing
   works at all.

6. **Prefer the response over a recorder when the pattern gives you one.** A request/reply test needs no
   synchronization at all, because `RequestAsync` completes when the handler has answered:

   ```csharp
   var response = await bus.RequestAsync(new GetProductRequest { ProductId = id }, cancellationToken);

   Assert.True(response.Found);
   Assert.Equal("Keyboard", response.Name);
   ```

   The same applies to a mediator command or query: dispatch and assert the result.

7. **Control time with `FakeTimeProvider`, never with real waiting.** Scheduling, saga timeouts and redelivery are all
   time-driven, and a test that waits 30 real minutes is not a test.

   ```csharp
   var time = new FakeTimeProvider(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
   builder.Services.AddSingleton<TimeProvider>(time);
   ```

   ```csharp
   await bus.SchedulePublishAsync(new PaymentReminderEvent { OrderId = id }, time.GetUtcNow().AddMinutes(10), ct);

   time.Advance(TimeSpan.FromMinutes(11));

   Assert.True(await recorder.WaitAsync(TimeSpan.FromSeconds(30)), "the scheduled message was not delivered");
   ```

   `FakeTimeProvider` comes from `Microsoft.Extensions.TimeProvider.Testing`. Keep two fixed windows in the test class:
   a generous one for "must arrive" and a short one for "must not arrive".

   Retry *timing* is the exception: configure a 1 ms backoff and assert the number of invocations rather than faking
   the clock.

8. **Assert attempt counts for retry behavior, with a counter the handler increments.** A handler that throws on the
   first attempt and succeeds on the second proves retry works; the recorder proves the message was ultimately handled
   and the counter proves how many attempts it took.

   ```csharp
   await using var host = await StartBusAsync(bus => bus
       .AddResilience(p => p.On<Exception>()
           .Retry(3, TimeSpan.FromMilliseconds(1), RetryBackoffType.Constant))
       .AddEventHandler<ThrowOnceHandler>(), ct);
   ```

9. **Test the error path explicitly.** A handler that throws should land on the error endpoint with fault headers. Bind
   a spy consumer to the fault endpoint and assert the headers as one value rather than probing them one by one.

10. **Use a real database for persistence features, and keep the transport in memory.** Outbox, inbox and saga-store
    behavior is database behavior: transactions, constraints, concurrency, row locking. A mocked store proves nothing
    about any of it. A real database with the InMemory transport isolates the storage concern from broker behavior,
    which is the right tier for those tests.

    Give every test its own database (or its own schema) so tests can run in parallel, and create it in a fixture
    shared across the class rather than per test.

11. **Use a real broker only for broker-specific semantics.** Connection recovery, acknowledgement behavior, topology
    conflicts, prefetch effects and queue-type characteristics need a container running the real thing. Everything
    else is faster and more deterministic on InMemory. Isolate broker tests per test (a vhost per test on RabbitMQ, a
    namespace or queue prefix elsewhere) so they do not leak topology into each other.

12. **Test a saga as a state machine, driven from its initiating message.** Start the bus with the saga registered and
    an in-memory or real saga store, send the initiating request, let the downstream handlers (or test stand-ins for
    them) reply, and assert the final response and the messages the saga dispatched.

    ```csharp
    var response = await bus.RequestAsync(
        new RequestQuickRefundRequest { OrderId = orderId, Amount = 49.99m, /* ... */ },
        cancellationToken);

    Assert.True(response.Success);
    Assert.Equal(orderId, response.OrderId);
    ```

    Cover at least: the happy path, each fault transition, and the timeout. Drive the timeout with `FakeTimeProvider`
    and a configured scheduling store, since a timeout with no store is skipped silently
    ([sagas.md](sagas.md)).

    Do not assert on intermediate saga state read from the store mid-flight. A transition's state is only committed
    when its transaction completes, so a read taken from inside the transition sees the previous value and a read taken
    from the test races the commit. Assert on what the saga *did*: the messages it sent and the response it produced.

13. **Keep one behavior per test, and name the behavior.**
    `Method_Should_Outcome_When_Condition`, `// arrange`, `// act`, `// assert` as single-line markers, and at most
    five `Assert.*` calls in a method. Past that, the test is checking several things: split it, or snapshot the
    resulting state as one value (CookieCrumble Markdown snapshots for multi-shape state, inline snapshots for a single
    value).

14. **Avoid `Assert.DoesNotContain` and other weak assertions.** They pass for the wrong reason as soon as the shape
    changes. Assert the complete collection or the complete value, so the test fails when the behavior changes rather
    than when it changes in one specific direction.

15. **Dispose the host.** `await using var host = ...` stops the runtime and the background workers; a leaked host keeps
    consuming and will interfere with the next test. Tests that share a broker or a database must also clean up the
    topology and data they created.

## Choosing the tier

| What you are testing | Tier |
| --- | --- |
| Handler logic for one message | InMemory transport, real host, recorder |
| Routing, fan-out, multiple handlers per event | InMemory transport, one recorder per handler |
| Request/reply contracts | InMemory transport, assert the response |
| Retry, redelivery, dead-lettering | InMemory transport, 1 ms backoff, counter and fault-endpoint spy |
| Outbox, inbox, saga store | Real database, InMemory transport |
| Scheduling and saga timeouts | `FakeTimeProvider` plus a scheduling store |
| Connection recovery, ack semantics, topology conflicts | Real broker in a container |
| A saga state machine | Real host with the saga registered, driven by its messages |

If a test needs extensive stubbing and reflection to reach the thing it is asserting, it is at the wrong tier.

## A note on `Mocha.Testing`

A dedicated testing package with a message tracker (deterministic completion waiting across cascading messages,
`ShouldHavePublished`/`ShouldHaveConsumed`-style assertions, stubbing for services that are not running) is in
development and is not part of a released version yet. Until it ships, the recorder and signal patterns above are the
supported way to write these tests. When it does ship, prefer it for new tests: it removes the hand-written recorder
and makes "wait for everything this message caused" a single call.

## Gotchas

- InMemory dispatch happens in process and can complete before `PublishAsync` returns, which makes some racy tests
  pass. The same test on a real transport will not be so forgiving, so keep the signal even when it looks unnecessary.
- Two InMemory buses in one process do not exchange messages. Simulate two services with one bus and two named
  InMemory transports.
- The InMemory scheduling store is not durable and does not retry a failed dispatch, so do not use it to test
  scheduler retry behavior.
- A handler that throws has not consumed the message: expect it on the error path, not in the recorder.
- Tests that assert on queue names are asserting on conventions derived from handler names and the service name. They
  break on a rename, which is sometimes exactly what you want and sometimes just noise.
