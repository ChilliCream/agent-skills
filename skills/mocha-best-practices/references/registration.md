# Registration and the source generator

Mocha discovers handlers, consumers and sagas at compile time with a Roslyn incremental source generator and emits one
registration method per assembly. The generated path is reflection-free and is the path with a backwards-compatibility
guarantee. Manual registration exists for what the generator cannot see.

## Rules

1. **Reference `Mocha.Analyzers` and call the generated method.** Nothing else registers handlers:

   ```csharp
   builder.Services
       .AddMessageBus()
       .AddOrderService()   // generated from [assembly: MessagingModule("OrderService")]
       .AddRabbitMQ();
   ```

   If the method does not exist in IntelliSense: the analyzer reference is missing or not marked as an analyzer, the
   project has not been rebuilt, there is no concrete handler in the assembly, or a diagnostic excluded every candidate
   (rule 4).

2. **Name the module explicitly.** Without the attribute the method name comes from the last segment of the assembly
   name, so `MyCompany.OrderService.Api` produces `AddApi()`, and a project rename changes the call site.

   ```csharp
   [assembly: MessagingModule("OrderService")]
   ```

   One `[MessagingModule]` per assembly. Use the bounded context name (`OrderService`, `Billing`, `Inventory`), not the
   layer. The mediator has its own attribute and its own generated method on the mediator builder:
   `[assembly: MediatorModule("OrderService")]` ([mediator.md](mediator.md)).

3. **Prefer the generator; use manual registration only for handlers it cannot see.** Plugin assemblies, dynamically
   loaded modules and test-only handlers are the legitimate cases:

   ```csharp
   builder.Services
       .AddMessageBus()
       .AddOrderService()                               // generated
       .AddEventHandler<ExternalNotificationHandler>()  // from another assembly
       .AddRequestHandler<PluginPaymentHandler>()       // from a plugin
       .AddRabbitMQ();
   ```

   Manual registration uses runtime reflection to build the consumer. Mixing is supported: registering the same handler
   both ways composes the configurations rather than duplicating the handler, which is how per-consumer configuration
   is layered on top of a generated registration:

   ```csharp
   builder.Services
       .AddMessageBus()
       .AddOrderService()
       .AddEventHandler<OrderPlacedHandler>(consumer =>
       {
           consumer.AddResilience(policy => policy.Default().Retry(10, TimeSpan.FromSeconds(2)).ThenRedeliver());
       })
       .AddRabbitMQ();
   ```

4. **Treat every `MO00xx` diagnostic as a registration failure, including the ones that are not errors.** A handler or
   saga that carries a diagnostic is excluded from code generation. MO0012 is `Info` and MO0013 is `Warning`, so the
   build is green and the handler is simply never wired up. "My handler is not being called" starts here.

   | Code | Severity | Means | Fix |
   | --- | --- | --- | --- |
   | MO0001 | Warning | A mediator command or query has no handler | Add the handler, or import the module that owns it |
   | MO0002 | Error | A mediator command or query has several handlers | Exactly one handler; use `INotification` for fan-out |
   | MO0003 | Warning | A mediator handler is abstract, so it is not registered | Put the interface on the concrete class |
   | MO0004 | Info | An open generic message type cannot be dispatched | Declare closed message types |
   | MO0005 | Error | A class implements several mediator handler interfaces | One handler class per interface |
   | MO0006 | Info | An open generic mediator handler cannot be auto-registered | Use a concrete handler, or register the closed form with `AddHandler<T>()` |
   | MO0011 | Error | A request type has several handlers | Exactly one `IEventRequestHandler` per request type |
   | MO0012 | Info | An open generic messaging handler cannot be auto-registered | Close the generic; the handler is otherwise never registered |
   | MO0013 | Warning | A messaging handler is abstract, so it is not registered | Put the interface on the concrete subclass |
   | MO0014 | Error | A saga has no public parameterless constructor | Remove constructor dependencies from the saga |
   | MO0015 | Error | AOT build with no `JsonContext` on the module | Add `JsonContext = typeof(...)` |
   | MO0016 | Error | A message, response or saga state type is missing from the context | Add `[JsonSerializable(typeof(T))]` |
   | MO0018 | Warning | A dispatched type is missing from the context | Add `[JsonSerializable(typeof(T))]` for the type and its response |
   | MO0020 | Warning | A mediator command or query is sent but has no handler in this assembly | Add the handler, or reference the module that owns it |

   MO0015, MO0016 and MO0018 only fire when `PublishAot` is `true`. The rest always fire. There are no code fixes, so
   none of these can be resolved with a lightbulb.

5. **Do not give a saga constructor dependencies.** The saga runtime instantiates sagas with `new()`, so a constructor
   parameter is MO0014, an error. Services a transition needs are resolved where the transition runs, not on the saga
   instance ([sagas.md](sagas.md)).

6. **Order the builder chain so intent reads top to bottom, and keep the transport last.**

   ```csharp
   builder.Services
       .AddMessageBus()
       .Host(h => h.ServiceName("order-service"))  // identity, before anything derives names from it
       .AddInstrumentation()                       // cross-cutting
       .AddResilience()
       .AddConcurrencyLimiter(o => o.MaxConcurrency = 10)
       .AddHealthCheck()
       .AddOrderService()                          // handlers, consumers, sagas
       .AddEntityFramework<OrderDbContext>(p =>    // persistence features
       {
           p.UsePostgresOutbox();
           p.UseResilience();
           p.UseTransaction();
           p.UsePostgresInbox();
       })
       .AddRabbitMQ();                             // transport
   ```

   This is a readability convention, not a hard requirement, and it makes a diff show what changed about the service
   rather than where in a chain a call landed.

7. **Keep registration in `Program.cs` or one extension method per concern.** A shared `AddDefaultMessagingSettings()`
   extension for instrumentation, resilience and concurrency keeps each service's chain down to what is specific about
   that service. Resist the opposite: a single helper that hides the transport and the handler module too, which makes
   the topology invisible at the call site.

8. **Registration is per assembly, and modules compose.** A service that consumes contracts and handlers from several
   assemblies calls each generated method:

   ```csharp
   builder.Services
       .AddMessageBus()
       .AddOrderService()
       .AddSharedNotifications()
       .AddRabbitMQ();
   ```

   The generated method carries `[MessagingModuleInfo(MessageTypes = ...)]`, which the generator in a downstream
   assembly reads to avoid registering serializers for types an imported module already covers. The array only
   advertises types that received generated serializer configuration, so it is empty outside AOT mode. Do not infer
   from a module import that a type's serialization is handled; see rule 9.

9. **Under AOT, the module needs a `JsonSerializerContext` that covers every type on the wire.**

   ```csharp
   [assembly: MessagingModule("OrderService", JsonContext = typeof(OrderServiceJsonContext))]

   [JsonSerializable(typeof(OrderPlacedEvent))]
   [JsonSerializable(typeof(ReserveInventoryCommand))]
   [JsonSerializable(typeof(GetProductRequest))]
   [JsonSerializable(typeof(GetProductResponse))]
   [JsonSerializable(typeof(RefundSagaState))]
   public partial class OrderServiceJsonContext : JsonSerializerContext;
   ```

   What must be listed: every handled message type, every response type, every saga state type, and every type the
   service publishes, sends or requests even if it never handles it. A type in the context with no local handler still
   gets registered, which is the supported way to make a producer-only contract AOT-safe.

   Two switches, independently: `JsonContext` turns on serializer code generation; the `PublishAot` MSBuild property
   turns on strict runtime mode and the MO0015, MO0016 and MO0018 diagnostics. A non-AOT build with a `JsonContext` is
   valid, and so is an AOT build whose types are all covered by imported modules.

10. **Do not depend on the shape of the generated code.** It registers message types, then sagas, then batch handlers,
    consumers, request handlers and event handlers, each group sorted by type name. That order is deterministic but it
    is an implementation detail, and so is everything else about the generated output. Code that reflects over it, or
    relies on handler invocation order derived from it, breaks on the next release.

## Consumer and endpoint configuration

Per-consumer configuration (resilience, endpoint settings) attaches to the handler registration, while queue and
topology concerns belong to the transport ([routing.md](routing.md)):

```csharp
builder.Services
    .AddMessageBus()
    .AddResilience()                              // bus default
    .AddOrderService()
    .AddEventHandler<NotificationHandler>(consumer =>
    {
        consumer.AddResilience(policy => policy.Default().Redeliver());
    })
    .AddRabbitMQ(transport =>
    {
        transport.Handler<OrderPlacedHandler>()
            .ConfigureEndpoint(ep => ep.MaxConcurrency(5).MaxPrefetch(50));
    });
```

A consumer-level policy replaces the bus-level and transport-level policy for that consumer; it does not merge.

## Gotchas

- A partial handler class that restates the interface on several parts is deduplicated and registers once. Restating is
  safe and is not MO0002 or MO0011.
- `AddSaga<T>()` by hand is unnecessary when the generated module is called; it already registers every concrete
  `Saga<TState>` subclass in the assembly.
- Abstract sagas and sagas declared as records are not discovered at all, with no diagnostic. A saga is a concrete
  class.
- MO0001 and MO0020 are suppressed when the handler lives in an imported mediator module, so a warning that disappears
  after adding a module call was telling the truth.
- `MO0007` to `MO0010`, `MO0017` and `MO0019` do not exist. A search result claiming otherwise is wrong.
