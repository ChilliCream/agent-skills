# Messages

A message is the published surface of a service. The code around it can be refactored at will; the shape on the wire
cannot, because a consumer in another process, possibly on an older deployment, is reading it. Treat a message type the
way you would treat a public API.

Mocha does not require a base class, a marker interface or attributes on a message. That freedom is the reason the rules
below are conventions rather than compiler errors.

## Rules

1. **A message is a sealed record of business data with `required` init-only properties.**

   ```csharp
   namespace MyApp.Contracts.Events;

   public sealed record OrderPlacedEvent
   {
       public required Guid OrderId { get; init; }
       public required string CustomerId { get; init; }
       public required decimal TotalAmount { get; init; }
       public required DateTimeOffset PlacedAt { get; init; }
   }
   ```

   `required` makes an incomplete message a compile error at every producer instead of a null at the consumer. Init-only
   properties stop a handler from mutating the message it was handed. Named properties are preferred over positional
   records for a cross-service contract: a positional record encodes parameter order into every construction site, so
   inserting a property silently re-binds arguments in a way the compiler may accept.

2. **No framework types in a message.** No `IConsumeContext`, no `DbContext` entity, no `ILogger`, no transport type, no
   `JsonDocument` carrying "whatever the producer felt like". A message holds values a consumer can act on: identifiers,
   amounts, timestamps, enum-like strings. If the payload needs the envelope, read the envelope
   (rule 7), do not duplicate it into the body.

   Do not reuse an EF Core entity as a message. The entity tracks navigation properties, lazy loading and database
   concerns; serializing it ships your schema to every consumer and makes a column rename a breaking contract change.

3. **Name by intent, and let the name tell the reader which pattern to use.**

   | Kind | Form | Examples |
   | --- | --- | --- |
   | Event | past tense, noun-verb | `OrderPlaced`, `PaymentCompleted`, `ShipmentShipped` |
   | Command (send) | imperative, verb-noun | `ReserveInventory`, `ProcessPayment`, `ScheduleShipment` |
   | Request (request/reply) | verb-noun, `Request`/`Response` pair | `GetProductRequest` / `GetProductResponse` |

   A name in the wrong form is a design smell worth reporting: `OrderPlacedCommand` says the producer is telling someone
   to do something about a fact, and `ReserveInventoryEvent` says a request is being broadcast. See
   [patterns.md](patterns.md) for the semantics each form commits you to.

4. **Contracts that cross a service boundary live in their own assembly.** One `*.Contracts` project with events,
   commands, requests and responses, referenced by producer and consumer, so both compile against the same type and a
   change is visible in a pull request. Organize by kind (`Events/`, `Commands/`, `Requests/`) and keep the namespace
   stable: the namespace is part of the wire identity (rule 8).

   A contracts assembly holds no handlers, no DbContext, no transport packages. It is types and nothing else.

5. **A request declares its response through `IEventRequest<TResponse>`.**

   ```csharp
   public sealed record GetProductRequest : IEventRequest<GetProductResponse>
   {
       public required Guid ProductId { get; init; }
   }
   ```

   This is the one marker interface worth having: it lets the compiler infer the response type at the call site
   (`await bus.RequestAsync(request, ct)` returns `ValueTask<GetProductResponse>`) and it constrains
   `IEventRequestHandler<TRequest, TResponse>` so a handler cannot be paired with the wrong response. A request type
   without it can only be dispatched through the untyped overload, which gives up both guarantees.

   A send message (fire and forget) deliberately does *not* implement it, because there is no response.

6. **A response is a type of its own, not a reused event.** `GetProductResponse` exists to answer
   `GetProductRequest`. Reusing an event as a response couples the reply contract to the pub/sub contract, so a change
   wanted by one breaks the other.

   Model "not found" and "failed" as fields on the response (`Found`, `Success`, `FailureReason`) when the caller is
   expected to handle them as outcomes. A handler returning `null` is not an option: the bus throws an
   `InvalidOperationException` for a null response.

7. **Envelope metadata is read from the envelope.** `MessageId`, `CorrelationId`, `ConversationId`, `CausationId`,
   `SentAt`, `SourceAddress`, `DeliveryCount` and custom headers all live on `IConsumeContext<T>`. Implement
   `IConsumer<T>` when a handler needs them:

   ```csharp
   public sealed class OrderAuditConsumer(ILogger<OrderAuditConsumer> logger)
       : IConsumer<OrderPlacedEvent>
   {
       public ValueTask ConsumeAsync(IConsumeContext<OrderPlacedEvent> context)
       {
           logger.LogInformation(
               "Audit {OrderId} message {MessageId} correlation {CorrelationId}",
               context.Message.OrderId,
               context.MessageId,
               context.CorrelationId);

           return default;
       }
   }
   ```

   Do not add a `CorrelationId`, `MessageId`, `Timestamp` or `TenantId` property to a message when the envelope already
   carries that value. A duplicated correlation id drifts from the real one and then nobody knows which to trust. The
   exception is a value with business meaning: `PlacedAt` (when the order was placed) is business data, `SentAt` (when
   the infrastructure dispatched the envelope) is not.

8. **Correlation is automatic: inherit it, do not rebuild it.** `ConversationId` groups everything in one business
   transaction, `CorrelationId` groups a workflow or saga instance, and `CausationId` points at the `MessageId` that
   caused this message. A message published from inside a handler inherits the conversation and correlation of the
   inbound message and gets its causation set automatically. Code that generates its own ids and threads them through
   payloads is re-implementing this, worse.

9. **Custom headers carry routing and infrastructure concerns, not business data.** A tenant discriminator, a priority
   hint or a schema marker belongs in `Headers`; the order total does not.

   ```csharp
   await bus.PublishAsync(
       new OrderPlacedEvent { /* ... */ },
       new PublishOptions
       {
           Headers = new() { ["x-tenant"] = "acme" }
       },
       cancellationToken);
   ```

   Header values must be portable: `null`, bool, string, number, object with string keys, or array. Mocha converts the
   common CLR types (numeric types, `Guid`, `Uri`, enums as member names, `DateTime`/`DateTimeOffset`/`DateOnly`/
   `TimeOnly` as ISO 8601, `TimeSpan` in constant format, byte sequences as base64). Anything else is not supported, so
   do not try to put an arbitrary object in a header. Use a string when the exact text matters, for example a
   high-precision decimal.

10. **The wire identity is derived from the CLR namespace and type name, so moving a type is a breaking change.**
    `MyApp.Contracts.OrderPlaced` becomes `urn:message:my-app.contracts:order-placed`, and the receiving side matches
    that URN against its registered types. Renaming the type or moving it to another namespace changes the URN, and
    consumers stop recognizing the message.

    When a refactor must move the type, pin the identity with `AddMessage<T>()` so the wire contract survives the
    code change:

    ```csharp
    builder.Services
        .AddMessageBus()
        .AddMessage<OrderPlacedEvent>(d =>
        {
            d.Send(route => route.ToQueue("orders-queue"));
        })
        .AddRabbitMQ();
    ```

    `AddMessage<T>()` is also how an explicit outbound route is configured; see [routing.md](routing.md).

11. **Evolve a contract additively.** Adding an optional `init` property with a default is backward compatible: a
    consumer that does not know the property ignores it. Renaming, removing or re-typing a property, or making an
    existing property `required`, is breaking and needs a coordinated deployment or a second message type.

    When both shapes must coexist, introduce a new type (`OrderPlacedEventV2`) rather than overloading the old one with
    mutually exclusive nullable fields, and retire the old type once every consumer has moved.

12. **A polymorphic message hierarchy is a deliberate choice.** A message that implements registered interfaces or
    extends registered base types carries all those identities in `EnclosedMessageTypes`, which lets a handler
    registered for the interface receive every implementation. That is useful for a deliberate "all order events" type
    of subscription, and surprising when a base type was introduced only to share properties. Share properties through
    composition or duplication across contracts before reaching for inheritance.

## AOT

Under `PublishAot`, every message type, response type and saga state type must be declared on the module's
`JsonSerializerContext`, including types the service only publishes and never handles. The generator enforces it
(MO0015, MO0016, MO0018); see [registration.md](registration.md).

## Gotchas

- `OrderPlaced`, `OrderPlacedEvent` and `OrderPlacedEventV2` in three different namespaces are three different wire
  identities. Decide on one and keep it.
- Suffix stripping in endpoint naming removes `Handler`, `Consumer`, `Command`, `Event`, `Message`, `Query` and
  `Response`, so `OrderPlacedEvent` and `OrderPlaced` resolve to the same endpoint name segment. Two contracts whose
  names differ only by such a suffix collide in the topology; see [routing.md](routing.md).
- A struct works as a message, but a record gives value equality, `with` expressions and a readable `ToString()` for
  logs. Use records unless there is a measured reason not to.
- `decimal` survives JSON round-trips as a number with full precision in .NET, but a non-.NET consumer may read it as a
  float. For money crossing a language boundary, agree on a representation (minor units as an integer, or a string).
