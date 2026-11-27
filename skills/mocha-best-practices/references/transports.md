# Transports

A transport connects Mocha to a broker: connections, topology provisioning, dispatch and receive. Handlers, patterns
and pipelines are identical across transports, so the transport is an infrastructure decision, not an application one.

| Transport | Package | Fits |
| --- | --- | --- |
| InMemory | `Mocha.Transport.InMemory` | Development, tests, single-process work |
| PostgreSQL | `Mocha.Transport.Postgres` | Database-backed messaging when you already run Postgres |
| RabbitMQ | `Mocha.Transport.RabbitMQ` | Distributed production systems |
| Azure Service Bus | `Mocha.Transport.AzureServiceBus` | Managed cloud messaging on Azure |

## Rules

1. **The transport is the last call in the chain, and it is one call.**

   ```csharp
   builder.Services
       .AddMessageBus()
       .AddOrderService()
       .AddRabbitMQ();
   ```

   Everything transport-specific goes in its configuration lambda, not spread across the chain.

2. **Never run the InMemory transport in production, and never conclude from it that the system works.** Messages live
   in process memory: they are lost on exit, cannot reach another process, and the transport does not exercise
   connection recovery, acknowledgement semantics, topology conflicts or broker-specific queue behavior. It is the
   right default for unit and integration tests of handler logic and routing, and nothing else.

3. **Pick the transport from the trade-off you can accept, and write the reason down.**

   - PostgreSQL: one fewer system to operate, transactional consistency with your data, native and cancellable
     scheduling. You pay with database capacity and a round trip per operation.
   - RabbitMQ: a real broker with durability and fan-out at scale. You pay with operating it (clustering, memory and
     disk alarms, queue types, upgrades) and it has no scheduling of its own.
   - Azure Service Bus: managed, durable, native scheduling and cancellation. You pay per use, and throughput depends
     on the namespace SKU.

4. **On RabbitMQ, register `UsePostgresScheduling()` if anything schedules.** RabbitMQ has no scheduling store, so a
   scheduled dispatch throws `NotSupportedException`, redelivery cannot reschedule, and saga timeouts are silently
   skipped. Those three features are easy to adopt without noticing the dependency
   ([scheduling.md](scheduling.md), [reliability.md](reliability.md)).

5. **Tune the receive settings that match the transport, and tune them per endpoint.** Prefetch on RabbitMQ and batch
   size on PostgreSQL both interact with the concurrency limit: a large prefetch with a low concurrency limit just
   parks messages in the consumer's buffer where other instances cannot take them.

   ```csharp
   .AddRabbitMQ(transport =>
   {
       transport.Handler<OrderPlacedHandler>()
           .ConfigureEndpoint(ep => ep.MaxPrefetch(50).MaxConcurrency(10));
   });
   ```

   ```csharp
   .AddPostgres(transport =>
   {
       transport.Handler<OrderPlacedHandler>()
           .ConfigureEndpoint(ep => ep.MaxBatchSize(100));
   });
   ```

6. **Keep the connection string and broker settings in configuration, not in code.** The transport call takes the
   values; the values come from `IConfiguration`, a connection string provider, or an Aspire client registration:

   ```csharp
   builder.AddRabbitMQClient("rabbitmq");       // Aspire, connection managed by the host
   builder.AddNpgsqlDbContext<OrderDbContext>("order-db");
   ```

7. **Use multiple transports only for a reason you can name.** A different durability or throughput profile for one
   message class is a reason: analytics firehose on one transport, business events on another. Claim the handler for
   the non-default transport and mark the default:

   ```csharp
   builder.Services
       .AddMessageBus()
       .AddOrderService()
       .AddRabbitMQ(r => r.IsDefaultTransport())
       .AddInMemory(m => m.Handler<ClickStreamHandler>());
   ```

   Each transport keeps its own connections, topology and pipeline, so multi-transport doubles the operational surface
   and the ways configuration can diverge ([routing.md](routing.md)).

8. **Let the transport provision topology, and know what happens when it must not.** Mocha resolves endpoints and
   builds exchanges, queues and bindings at startup, so a bad configuration fails at startup rather than silently
   later. In an environment where the service is not allowed to declare topology, provision it out of band and bind
   explicitly, accepting that a message to a non-existent destination is dropped by the broker.

9. **Add the health check and wire it to your probes.** `Mocha.Hosting` sends a `HealthRequest` through the bus and
   waits for the reply, which exercises serialization, transport, routing and handler execution rather than just
   checking that a socket is open.

   ```csharp
   builder.Services
       .AddMessageBus()
       .AddHealthCheck()      // registers the HealthRequestHandler
       .AddOrderService()
       .AddRabbitMQ();

   builder.Services.AddHealthChecks().AddMessageBus();
   ```

   ```csharp
   app.MapHealthChecks("/health/ready", new() { Predicate = c => c.Tags.Contains("ready") });
   app.MapHealthChecks("/health/live", new() { Predicate = c => c.Tags.Contains("live") });
   ```

   The check is registered with the `ready` and `live` tags. Pass a URI to target a specific endpoint when several
   transports or services are in play.

10. **Set endpoint concurrency and prefetch per deployment shape, not once and forever.** Ten instances with a
    concurrency limit of ten is a hundred concurrent handlers against the same database. Size the total, not the
    per-instance number.

## Transport-specific notes worth remembering

- **InMemory** dispatches in process, so delivery completes before `PublishAsync` returns. That makes tests fast and it
  hides every timing problem a real broker will show you.
- **PostgreSQL** keeps messages in tables, which means your database monitoring also covers the message backlog, and
  your database capacity planning now includes it.
- **RabbitMQ** writes faulted messages to the `_error` queue of each endpoint by convention; check there first when a
  handler throws.
- **Azure Service Bus** maps `Temporary()` to `AutoDeleteOnIdle` with a 24-hour default, and `Temporary(TimeSpan)` sets
  the idle window.

## Gotchas

- Swapping the transport does not change handler code, but it does change queue and exchange names, scheduling
  support, and what `Temporary()` means. Re-read [routing.md](routing.md) and [scheduling.md](scheduling.md) after a
  swap.
- Two InMemory buses in one process do not exchange messages. To simulate two services in one process, use one bus with
  two named InMemory transports.
- Transport-level resilience and concurrency settings replace the bus-level ones for that transport rather than merging
  with them.
