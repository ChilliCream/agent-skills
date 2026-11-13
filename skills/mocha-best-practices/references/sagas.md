# Sagas

A saga is a process manager: a state machine that coordinates several messages over time, keeps persisted state between
them, and drives a business process to completion. Mocha sagas are orchestration, not choreography: one state machine
issues commands and reacts to the replies.

A saga is also the most expensive abstraction in this framework. It adds persisted state, correlation, concurrency and
a whole class of "stuck instance" failures. Use one when the process genuinely needs it.

## When a saga is the right tool

| Situation | Handler | Saga |
| --- | --- | --- |
| One message in, one action out | yes | no |
| Completes in one step | yes | no |
| Spans several messages over time | no | yes |
| Must wait for replies before proceeding | no | yes |
| Coordinates parallel operations | no | yes |
| State must survive a restart | no | yes |
| Responds to the original caller after several steps | no | yes |

A handler that sends a command and does not need to wait for the reply is a handler. The moment the process must pause
and resume on a future message, it is a saga.

## Rules

1. **Saga state extends `SagaStateBase` and holds only what transitions need.** `SagaStateBase` provides `Id`, `State`,
   `Errors` and `Metadata`. Capture the initiating data as `required` init-only properties, and make only the fields
   that transitions update mutable:

   ```csharp
   public class RefundSagaState : SagaStateBase
   {
       public required Guid OrderId { get; init; }
       public required decimal Amount { get; init; }
       public required string CustomerId { get; init; }

       public Guid? RefundId { get; set; }
       public decimal? RefundedAmount { get; set; }
       public string? FailureReason { get; set; }

       public static RefundSagaState FromQuickRefund(RequestQuickRefundRequest request)
           => new()
           {
               OrderId = request.OrderId,
               Amount = request.Amount,
               CustomerId = request.CustomerId
           };

       public ProcessRefundCommand ToProcessRefund()
           => new() { OrderId = OrderId, Amount = Amount, CustomerId = CustomerId };
   }
   ```

   Saga state is persisted and serialized, so treat it as a contract too: it must survive a deployment that changes the
   saga. Add properties, do not rename or remove them while instances are in flight.

   Factory and projection methods on the state (`FromQuickRefund`, `ToProcessRefund`) keep the state machine
   declaration readable. Keep them pure: no services, no I/O.

2. **A saga has no constructor dependencies.** The runtime instantiates sagas with `new()`, so a constructor parameter
   is a build error (MO0014). A saga that needs to call a service is doing work that belongs in a handler: send a
   command and react to the reply.

3. **Declare state names as constants.** String literals in `During("AwaitingRefund")` drift, and a typo creates a
   state nothing can leave.

   ```csharp
   private const string AwaitingRefund = nameof(AwaitingRefund);
   private const string Completed = nameof(Completed);
   ```

4. **Every state needs a path to a final state.** `Finally(name)` declares a final state: entering it deletes the
   persisted state and sends the optional response. A state with no outgoing transition and no `Finally` is a leak that
   accumulates rows and never completes.

   ```csharp
   protected override void Configure(ISagaDescriptor<RefundSagaState> descriptor)
   {
       descriptor
           .Initially()
           .OnRequest<RequestQuickRefundRequest>()
           .StateFactory(RefundSagaState.FromQuickRefund)
           .Send((_, state) => state.ToProcessRefund())
           .TransitionTo(AwaitingRefund);

       descriptor
           .During(AwaitingRefund)
           .OnReply<ProcessRefundResponse>()
           .Then((state, response) =>
           {
               if (response.Success)
               {
                   state.RefundId = response.RefundId;
                   state.RefundedAmount = response.Amount;
               }
               else
               {
                   state.FailureReason = response.FailureReason ?? "Refund processing failed";
               }
           })
           .TransitionTo(Completed);

       descriptor
           .Finally(Completed)
           .Respond(state => new QuickRefundResponse
           {
               OrderId = state.OrderId,
               Success = state.RefundId.HasValue,
               RefundId = state.RefundId,
               FailureReason = state.FailureReason,
               CompletedAt = DateTimeOffset.UtcNow
           });
   }
   ```

5. **Give every saga a timeout.** A saga waiting for a reply that never arrives waits forever. `Timeout(TimeSpan)`
   creates a timed-out final state, schedules the deadline when the instance is created, and cancels it automatically
   when any final state is reached.

   ```csharp
   descriptor.Timeout(TimeSpan.FromMinutes(30))
       .Respond(state => new OrderTimedOutResponse(state.Id));

   descriptor.DuringAny()
       .OnTimeout()
       .TransitionTo(StateNames.TimedOut);
   ```

   `DuringAny().OnTimeout()` is the recommended pattern: it handles the timeout from whichever state the saga is in. A
   configured `Timeout()` with no `OnTimeout()` handler for the current state throws a `SagaExecutionException`.

   A per-state deadline is an on-entry action instead:

   ```csharp
   descriptor.During(AwaitingRefund)
       .OnEntry()
       .ScheduleTimeout(TimeSpan.FromMinutes(5));
   ```

   The scheduled timeout is cancelled automatically when the saga leaves the state normally.

6. **Verify the scheduling store, or the timeout silently does not exist.** `Timeout()` schedules through the same
   store as `ScheduleSendAsync`. If no store is available when the instance is created, the timeout is skipped with no
   error: the saga runs with no deadline. On RabbitMQ that means `UsePostgresScheduling()` is required
   ([scheduling.md](scheduling.md)). "The timeout never fires" is almost always a missing store.

7. **A fired timeout must advance the saga.** Transition to a compensation state and undo completed work, or to a final
   state with a failure response. Never leave the saga in the state it timed out in, and only re-send the original
   command if that command is idempotent.

8. **Handle faults where you wait for a reply.** A command that fails sends back a `NotAcknowledgedEvent` instead of
   the expected reply, which `OnFault()` catches:

   ```csharp
   descriptor
       .During(AwaitingRefund)
       .OnFault()
       .Then((state, fault) =>
       {
           state.FailureReason = fault.ErrorMessage;
       })
       .Send((_, state) => new ReverseChargeCommand { OrderId = state.OrderId, Amount = state.Amount })
       .TransitionTo(Compensating);
   ```

   The fault carries `ErrorCode`, `ErrorMessage`, `CorrelationId` and `MessageId`. A saga that sends commands from
   several waiting states needs an `OnFault()` in each of them, otherwise a failure in one branch strands the instance.

9. **Compensate, do not roll back.** There is no distributed transaction. Classify each step as compensable (can be
   undone), pivot (the point of no return) or retryable (will eventually succeed), and write the undo commands for the
   compensable ones. Put the pivot as late in the process as possible.

10. **Model parallel waits as explicit states, one per combination of completed replies.** Do not try to count replies
    in state fields and branch inside `Then`.

    ```csharp
    descriptor.During(AwaitingInspection)
        .OnReply<InspectReturnResponse>()
        .Then((state, response) => state.InspectionResult = response.Result)
        .Send((_, state) => state.ToRestockInventory())
        .Send((_, state) => state.ToProcessRefund())
        .TransitionTo(AwaitingBothReplies);

    descriptor.During(AwaitingBothReplies)
        .OnReply<RestockInventoryResponse>()
        .Then((state, response) => state.InventoryRestocked = response.Success)
        .TransitionTo(RestockDoneAwaitingRefund);

    descriptor.During(AwaitingBothReplies)
        .OnReply<ProcessRefundResponse>()
        .Then((state, response) => state.RefundId = response.RefundId)
        .TransitionTo(RefundDoneAwaitingRestock);

    // each "one done" state has a transition for the other reply into Completed
    ```

    Whichever reply arrives first moves the saga into a "one done" state, and the second completes it. The state machine
    then has no race to lose, and no locking to get wrong.

11. **Do not assume which message starts the process.** Delivery order is not guaranteed. If two message types could
    legitimately initiate the saga, configure both as initial transitions. The first message you wrote in code is not
    necessarily the first the saga receives in production.

12. **Choose `OnRequest` or `OnEvent` by whether somebody is waiting.** `OnRequest<T>()` captures the reply address so
    a final state can `Respond(...)` to the original caller. `OnEvent<T>()` does not, and a saga started by an event
    sends no response when it completes. Picking `OnEvent` and then expecting `Respond` to reach a caller is a common
    mistake.

13. **Configure a persistent store before production.** Saga state is in memory by default and is lost on restart,
    which turns every deployment into a set of stranded processes.

    ```csharp
    public class CatalogDbContext : DbContext
    {
        public DbSet<SagaState> SagaStates => Set<SagaState>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.AddPostgresSagas();
        }
    }
    ```

    ```csharp
    builder.Services
        .AddMessageBus()
        .AddCatalog()
        .AddEntityFramework<CatalogDbContext>(p => p.AddPostgresSagas())
        .AddRabbitMQ();
    ```

    Then create and apply the EF Core migration.

14. **Keep side effects in the declaration, not in `Then`.** `Then` updates state. `Send`, `Publish`, `ScheduledSend`
    and `ScheduledPublish` dispatch messages, and the runtime handles their ordering, correlation (including the
    `saga-id` header) and their participation in the saga transaction. Dispatching a message by hand from inside a
    `Then` bypasses all of that.

15. **Let correlation work for you.** A reply to a command the saga sent is matched through the `saga-id` header.
    An event that is not a reply is matched by implementing `ICorrelatable`:

    ```csharp
    public sealed record SagaTimedOutEvent(Guid SagaId) : ICorrelatable
    {
        public Guid? CorrelationId => SagaId;
    }
    ```

    The lookup order is `ICorrelatable.CorrelationId`, then the `saga-id` header, then "treat as initiating and create
    a new instance". That last step is why an uncorrelated event aimed at an existing saga creates a new one instead,
    which looks like duplicated processes.

## Concurrency

Two messages for the same instance arriving together are resolved by optimistic concurrency in the Postgres store: the
second writer sees the version mismatch, reloads and retries the transition. This is automatic. A saga endpoint under
very high concurrency for one instance can build up retry contention, in which case host it on its own endpoint with a
constrained concurrency limit ([routing.md](routing.md)).

## Testing

A saga is a state machine, so test it as one: feed it events in order and assert the resulting state and the messages it
dispatched, without a broker. See [testing.md](testing.md).

## Gotchas

- "No transition defined" means a message arrived in a state with no matching transition. Add the transition, or a
  `DuringAny()` catch-all, and consider whether the message should have been correlated to that instance at all.
- A saga declared as a record or as an abstract class is not discovered, with no diagnostic.
- `Finally` deletes the persisted state. Anything you need after completion must be written somewhere else first, for
  example by publishing an event from the final transition.
- A timeout that fires after the instance was deleted is dropped silently.
- `Saga.Create<TState>(...)` builds a saga from a lambda, which is useful in tests and prototypes. Production sagas are
  classes, so they are discovered by the generator and are reviewable as a unit.
