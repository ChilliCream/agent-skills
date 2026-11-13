# Observability

In a messaging system, "where did the message go?" is the question you will ask most, and it is unanswerable from logs
alone. Mocha emits OpenTelemetry spans and metrics for every dispatch, receive and consume, and propagates trace context
across service boundaries, but only if instrumentation is switched on.

## Rules

1. **Both halves are required.** `AddInstrumentation()` activates the diagnostic observer; the OpenTelemetry SDK only
   collects from sources it is subscribed to. Either one alone produces nothing.

   ```csharp
   builder.Services
       .AddMessageBus()
       .AddInstrumentation()
       .AddOrderService()
       .AddRabbitMQ();

   builder.Services
       .AddOpenTelemetry()
       .WithTracing(tracing => tracing
           .AddSource("Mocha")
           .AddAspNetCoreInstrumentation()
           .AddHttpClientInstrumentation())
       .WithMetrics(metrics => metrics
           .AddMeter("Mocha")
           .AddAspNetCoreInstrumentation());
   ```

   Without `AddInstrumentation()` Mocha uses a no-op observer with zero overhead, which is also zero visibility.

2. **Instrument the mediator too.** `AddMediator()` always installs `MediatorDiagnosticMiddleware`, but with a no-op
   listener until `AddInstrumentation()` is called on the mediator builder. A trace that covers the HTTP request and
   the bus but skips the in-process command dispatch has a hole exactly where the business logic is
   ([mediator.md](mediator.md)).

3. **Expect three linked spans per message flow.** `publish {destination}` on the producer, `receive {endpoint}` on the
   consumer, and `consumer {handler}` as a child of receive. If the receive span is not a child of the publish span,
   trace context is not propagating: check that both services call `AddInstrumentation()` and that no custom
   middleware or transport hop strips headers.

4. **Do not hand-roll correlation.** Context propagates through `traceparent` and `tracestate` headers in the W3C Trace
   Context format, written by the dispatch instrumentation and read by the receive instrumentation, the same format
   ASP.NET Core and `HttpClient` use. A custom correlation header threaded through payloads duplicates this badly, and
   Mocha's own `ConversationId`, `CorrelationId` and `CausationId` already cover the business-level view
   ([messages.md](messages.md)).

5. **Use the service name you configured, consistently.** `Host(h => h.ServiceName(...))` also drives queue naming
   ([routing.md](routing.md)), and the OpenTelemetry resource service name should match it. Two different names for the
   same service make a trace and a queue impossible to line up during an incident.

6. **Enable log and trace correlation rather than logging ids by hand.** With OpenTelemetry logging configured, every
   `ILogger` entry written inside a handler or middleware carries `TraceId` and `SpanId`:

   ```csharp
   builder.Logging.AddOpenTelemetry(logging =>
   {
       logging.IncludeFormattedMessage = true;
       logging.IncludeScopes = true;
   });
   ```

7. **Log with structured templates and business keys.** `logger.LogInformation("Invoice created for order {OrderId}",
   message.OrderId)` is queryable; an interpolated string is not. Log the business identity (order id, customer id),
   not the whole message, and never secrets or personal data that the message happens to carry.

8. **Do not log what the pipeline already records.** A "received message X" log line at the top of every handler
   duplicates the receive and consume spans, multiplies log volume by the message rate, and still tells you less than
   the trace does. Log decisions and outcomes, not arrivals.

9. **Alert on the error and skipped queues and on the critical dead-letter log.** Traces explain a failure after you
   know it happened; those two queues are how you find out. See [reliability.md](reliability.md).

10. **Write a custom `IBusDiagnosticObserver` only for a backend OpenTelemetry cannot reach.** Each method is called at
    the start of a stage and returns an `IDisposable` the pipeline disposes when the stage ends, which is what makes
    duration measurement possible:

    ```csharp
    public sealed class CustomDiagnosticObserver : IBusDiagnosticObserver
    {
        public IDisposable Dispatch(IDispatchContext context) => /* ... */;
        public IDisposable Receive(IReceiveContext context) => /* ... */;
        public IDisposable Consume(IConsumeContext context) => /* ... */;

        public void OnDispatchError(IDispatchContext context, Exception exception) { /* ... */ }
        public void OnReceiveError(IReceiveContext context, Exception exception) { /* ... */ }
        public void OnConsumeError(IConsumeContext context, Exception exception) { /* ... */ }
    }
    ```

    ```csharp
    builder.Services.AddSingleton<IBusDiagnosticObserver, CustomDiagnosticObserver>();
    ```

    Keep it allocation-light and non-blocking: it runs on every message, in the hot path. Never do I/O in it.

11. **Inspect the topology instead of guessing it.** `IMessagingRuntime.Description` is the live snapshot of endpoints,
    routes and bindings, and the Nitro topology view renders the same data with traces overlaid. For "my message is not
    arriving", the topology answers it faster than reading conventions.

## Nitro

Connecting a Mocha service to Nitro gives a managed backend for the same OpenTelemetry data plus the topology view:

```csharp
builder.Services.AddNitro().AddOpenTelemetry();
```

Credentials come from `NITRO_API_KEY`, `NITRO_API_ID` and `NITRO_STAGE`. The activity source and meter subscriptions are
still required.

## Gotchas

- Mocha follows the OpenTelemetry messaging semantic conventions, which are still in development status, so attribute
  names can change between OTel releases. Do not build alerts that depend on an exact attribute name without checking
  it after an upgrade.
- `AddInstrumentation()` on the bus does not instrument the mediator, and the reverse is also true.
- Spans are only emitted for pipelines that actually run. A handler whose registration was skipped by a generator
  diagnostic produces no spans at all, which looks exactly like a routing problem
  ([registration.md](registration.md)).
- Metrics and traces come from the same observer; subscribing to the source but not the meter gives traces with no
  throughput data, which is the half that answers capacity questions.
