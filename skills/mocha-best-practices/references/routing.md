# Routing and endpoints

An endpoint is a transport address plus a pipeline. Mocha derives every endpoint from your handler and message types at
startup, builds the broker topology then, and fails there rather than on the first message. Most services never
configure routing, and the ones that do should configure as little as possible.

## Rules

1. **Set the service name explicitly.**

   ```csharp
   builder.Services
       .AddMessageBus()
       .Host(h => h.ServiceName("order-service"))
       .AddOrderService()
       .AddRabbitMQ();
   ```

   The service name prefixes every subscribe (event) endpoint, which is what gives each subscribing service its own
   queue for a fan-out event. Without it the name falls back to `SERVICE_NAME`, `OTEL_SERVICE_NAME` or the entry
   assembly name, so a project rename or a missing environment variable silently renames queues, and renamed queues
   mean the old ones keep the backlog.

   Two services sharing a subscribe queue name is not a cosmetic problem: they become competing consumers and each sees
   roughly half the events.

2. **Learn the conventions before overriding them.** PascalCase becomes kebab-case and the suffixes `Handler`,
   `Consumer`, `Command`, `Event`, `Message`, `Query` and `Response` are stripped.

   | Route kind | Name derived from | Example |
   | --- | --- | --- |
   | Subscribe (events) | service name + handler name | `catalog.order-placed` |
   | Send and request | message type, no service prefix | `reserve-inventory` |
   | Publish (dispatch) | message namespace + name | `demo.contracts.events.order-placed` |
   | Error | `{endpoint}_error` | `catalog.order-placed_error` |
   | Skipped | `{endpoint}_skipped` | `catalog.order-placed_skipped` |
   | Reply | `response-{guid:N}` | temporary, per instance |

   A send endpoint is intentionally shared across services: there is one `reserve-inventory` queue and one owner. A
   subscribe endpoint is intentionally per service.

3. **Name handlers for what they do, because the handler name becomes the queue name.** `OrderPlacedHandler` in the
   `billing` service produces `billing.order-placed`. A handler called `Handler1` or `OrderStuffHandler` produces a
   queue an operator cannot interpret at 3am. Renaming a handler renames its queue, which is a deployment event: the
   new queue starts empty and the old one must be drained before removal.

4. **Prefer explicit outbound routes over hoping the convention matches.** When a message must land on a queue or
   exchange whose name you do not control, declare it:

   ```csharp
   builder.Services
       .AddMessageBus()
       .AddMessage<OrderPlacedEvent>(m => m.Publish(r => r.ToExchange("custom-orders-exchange")))
       .AddMessage<ProcessPaymentCommand>(m => m.Send(r => r.ToQueue("payment-processing-queue")))
       .AddRabbitMQ();
   ```

   An explicit route wins over the convention. `ToQueue` is point-to-point, `ToExchange` is fan-out, `ToTopic` is
   topic-based routing, and the transport maps those to its native concepts.

5. **Do not pass an address at the call site for routing that is really configuration.**

   ```csharp
   // wrong: the destination of every ReserveInventoryCommand is now spread across call sites
   await bus.SendAsync(command, new SendOptions { Endpoint = new Uri("queue:custom-inventory-queue") }, ct);
   ```

   `SendOptions.Endpoint` is for genuinely dynamic destinations (a tenant-specific queue resolved at runtime, replying
   into a queue another service owns). A fixed destination belongs in `AddMessage<T>()` where it is visible once.

   A per-call address also bypasses provisioning: the host records the queue with auto-provisioning disabled and never
   declares, binds or consumes it, and a message the broker cannot route is dropped. Silently.

6. **Use `Queue(...)` when the receive topology needs to change, and change only what you must.**

   ```csharp
   builder.Services
       .AddMessageBus()
       .AddOrderService()
       .AddRabbitMQ(transport =>
       {
           transport.BindExplicitly();

           transport.Queue("order-processing")
               .BindImplicitly()
               .MaxConcurrency(5)
               .FaultEndpoint("order-errors")
               .SkippedEndpoint("order-skipped")
               .Handler<OrderPlacedHandler>()
               .Handler<PaymentReceivedHandler>();
       });
   ```

   `Queue("name")` alone declares infrastructure. It becomes a receive endpoint as soon as it gets a `Handler<T>()`,
   `Consumer<T>()` or `Receives<T>()`. Putting several handlers on one queue is a real decision: they share the queue,
   the concurrency limit and the backlog, so one slow handler delays the others.

7. **Know which binding scope you are changing.** `BindImplicitly()` is the default at transport scope and
   auto-discovers handlers into convention-named queues with convention bindings.

   - `transport.BindExplicitly()`: the queues you declare are the complete receive topology. Any handler you forget to
     bind is simply not consumed.
   - `queue.BindImplicitly()`: custom queue name, Mocha still creates the source bindings for that queue's message
     types. This is the common combination.
   - `queue.BindExplicitly()`: you supply the bindings, for example with `BindFrom(...)` and a routing key.

   ```csharp
   transport.BindExplicitly();

   transport.Queue("regional-orders")
       .BindExplicitly()
       .BindFrom(new Uri("exchange:region-events"), "eu.*")
       .Handler<OrderPlacedHandler>();
   ```

8. **Use `Receives<T>()` when the queue is about a message type, `Handler<T>()` when it is about a handler.**
   `Receives<OrderPlaced>()` connects every registered handler for that type to the queue; `Handler<OrderPlacedHandler>()`
   binds one named handler. A `Receives<T>()` with no registered handler for `T` throws at startup, which is the
   correct outcome.

9. **Claim handlers per transport instead of reaching for explicit binding in a multi-transport setup.**

   ```csharp
   builder.Services
       .AddMessageBus()
       .AddOrderService()
       .AddRabbitMQ(r => r.IsDefaultTransport())
       .AddInMemory(m => m.Handler<AuditHandler>());
   ```

   A claimed handler belongs to the claiming transport; everything unclaimed falls through to the default. This stays
   readable as transports are added, where `BindExplicitly()` plus hand-declared queues does not.

10. **Configure an endpoint through `ConfigureEndpoint` rather than guessing its name.**

    ```csharp
    builder.Services
        .AddMessageBus()
        .AddOrderService()
        .AddRabbitMQ(rabbit =>
        {
            rabbit.Handler<OrderPlacedHandler>()
                .ConfigureEndpoint(ep => ep.MaxConcurrency(5).MaxPrefetch(50));
        });
    ```

    `Handler<T>()` keeps the convention-derived name and hands you the endpoint. Hard-coding the derived name
    (`"billing.order-placed"`) in configuration couples your settings to the handler's class name.

11. **Keep the error and skipped endpoints, and watch them.** Every receive endpoint gets `_error` (handler threw, with
    `fault-exception-type`, `fault-message`, `fault-stack-trace` and `fault-timestamp` headers) and `_skipped` (nothing
    consumed the message). A system with no alert on those queues has no failure detection; see
    [reliability.md](reliability.md). If no error endpoint is configured for a receive endpoint, the dead-letter
    middleware is not active at all, and the message is simply lost.

12. **`Temporary()` is a lifecycle intent for infrastructure, not a message TTL.** It scopes the queue to the lifetime
    of the process: `AutoDeleteOnIdle` on Azure Service Bus, a non-durable auto-delete queue on RabbitMQ, an
    `AutoDelete` row on PostgreSQL, API parity only on InMemory.

    ```csharp
    transport.Queue($"tenant-events-{instanceId}")
        .Temporary()
        .Receives<TenantEvent>();
    ```

    Mocha never appends an instance identity for you: `Queue(name)` uses exactly the string you pass. Pass a
    per-instance name and every instance gets its own copy of every published message; pass the same name and the
    instances compete for messages on one queue. Decide which you want and make the name say so.

13. **Scope precedence is bus, then transport, then endpoint, most specific wins.** Middleware, concurrency limits,
    circuit breakers and resilience policies all resolve this way, and the pipeline is compiled per endpoint from the
    three layers. Set the global default at the bus, deviate at the transport, and reserve endpoint-level overrides for
    the one endpoint that is genuinely different.

## Verifying the topology

Topology resolves at startup, so a wrong exchange name or an incompatible binding is a startup failure, not a lost
message later. Check the resulting topology instead of reasoning about conventions: the Nitro topology visualizer shows
every endpoint, route and binding, and `IMessagingRuntime.Description` exposes the same snapshot in process. When a
message goes somewhere unexpected, look at the topology first, then at `AddMessage<T>()` registrations, then at the
conventions.

## Gotchas

- Two contract types whose names differ only by a stripped suffix (`OrderPlaced` and `OrderPlacedEvent`) resolve to the
  same endpoint name segment and collide.
- The reply endpoint is managed by Mocha for request/reply correlation and is not something you declare. `Temporary()`
  is for your own queues.
- A handler whose registration was skipped by a generator diagnostic produces no endpoint at all, so its queue simply
  does not exist. See [registration.md](registration.md).
- Changing a service name, a handler name or a contract namespace all rename broker entities. Plan the drain, do not
  discover it in production.
