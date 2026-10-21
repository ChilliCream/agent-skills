# Saga foundation

## Goal

Add a state machine that drives a multi-message process to completion, with a deadline and a defined outcome for every
failure.

## Agent enforcement rules

### Do

- Confirm a saga is actually needed: the process must pause and resume on a future message.
- Derive state from `SagaStateBase`, with `required` init-only properties for the initiating data.
- Declare state names as `private const string` using `nameof`.
- Give every state a transition to a final state, and give the saga a `Timeout`.
- Dispatch messages with `Send`, `Publish`, `ScheduledSend` and `ScheduledPublish` in the declaration, not inside
  `Then`.

### Do not

- Do not give a saga constructor dependencies. The runtime instantiates it with `new()`, so a parameter is `MO0014`, a
  build error.
- Do not call a service from a saga. Send a command and react to the reply.
- Do not count replies in state fields and branch inside `Then`. Model each combination as its own state.
- Do not leave a timed-out saga in the state it timed out in.
- Do not rename or remove a state property while instances are in flight. Saga state is persisted, so it is a contract.

## Build checklist

- Define the state: `SagaStateBase` plus the business fields, with pure factory and projection methods to keep the
  declaration readable.
- Define the state name constants.
- Write `Configure(ISagaDescriptor<TState>)`: `Initially()`, one `During(state)` block per waiting state, `Finally()`
  for each terminal outcome.
- Add `OnFault()` in every state that waits for a reply to a command the saga sent.
- Add `Timeout(TimeSpan)` plus `DuringAny().OnTimeout()`.
- Register the saga store and apply the migration.
- Verify the scheduling store exists, or the timeout silently does not.

## Example: state and state machine

```csharp
public class RefundSagaState : SagaStateBase
{
    public required Guid OrderId { get; init; }
    public required decimal Amount { get; init; }
    public required string CustomerId { get; init; }

    public Guid? RefundId { get; set; }
    public string? FailureReason { get; set; }

    public static RefundSagaState FromRequest(RequestQuickRefundRequest request)
        => new()
        {
            OrderId = request.OrderId,
            Amount = request.Amount,
            CustomerId = request.CustomerId
        };

    public ProcessRefundCommand ToProcessRefund()
        => new() { OrderId = OrderId, Amount = Amount, CustomerId = CustomerId };
}

public sealed class RefundSaga : Saga<RefundSagaState>
{
    private const string AwaitingRefund = nameof(AwaitingRefund);
    private const string Compensating = nameof(Compensating);
    private const string Completed = nameof(Completed);

    protected override void Configure(ISagaDescriptor<RefundSagaState> descriptor)
    {
        descriptor
            .Initially()
            .OnRequest<RequestQuickRefundRequest>()
            .StateFactory(RefundSagaState.FromRequest)
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
                }
                else
                {
                    state.FailureReason = response.FailureReason ?? "Refund processing failed";
                }
            })
            .TransitionTo(Completed);

        descriptor
            .During(AwaitingRefund)
            .OnFault()
            .Then((state, fault) => state.FailureReason = fault.ErrorMessage)
            .Send((_, state) => new ReverseChargeCommand { OrderId = state.OrderId, Amount = state.Amount })
            .TransitionTo(Compensating);

        descriptor
            .During(Compensating)
            .OnReply<ReverseChargeResponse>()
            .TransitionTo(Completed);

        descriptor
            .Finally(Completed)
            .Respond(state => new QuickRefundResponse
            {
                OrderId = state.OrderId,
                Success = state.RefundId.HasValue,
                RefundId = state.RefundId,
                FailureReason = state.FailureReason
            });

        descriptor
            .Timeout(TimeSpan.FromMinutes(30))
            .Respond(state => new QuickRefundResponse
            {
                OrderId = state.OrderId,
                Success = false,
                FailureReason = "Refund timed out"
            });

        descriptor
            .DuringAny()
            .OnTimeout()
            .TransitionTo(StateNames.TimedOut);
    }
}
```

`Finally(name)` deletes the persisted state and sends the optional response. A waiting state with no outgoing
transition and no `Finally` accumulates rows that never complete.

## Example: persistence and registration

```csharp
public class OrderDbContext(DbContextOptions<OrderDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.AddPostgresSagas();
    }
}

builder.Services
    .AddMessageBus()
    .Host(h => h.ServiceName("order-service"))
    .AddResilience()
    .AddOrderService()
    .AddEntityFramework<OrderDbContext>(p =>
    {
        p.AddPostgresSagas();
        p.UsePostgresScheduling();   // required on RabbitMQ, or Timeout() never fires
        p.UseTransaction();
    })
    .AddRabbitMQ();
```

Then create and apply the EF Core migration. A missing migration is a runtime failure on the first saga instance, not a
build error.

## Example: per-state deadline

```csharp
descriptor
    .During(AwaitingRefund)
    .OnEntry()
    .ScheduleTimeout(TimeSpan.FromMinutes(5));
```

Cancelled automatically when the saga leaves the state normally. Use this when one step needs a tighter deadline than
the whole process.

## Guardrails

- A configured `Timeout()` with no `OnTimeout()` transition for the current state throws `SagaExecutionException`.
  `DuringAny().OnTimeout()` is the safe default.
- Correlation: a reply to a command the saga sent is matched through the `saga-id` header. An event that is not a reply
  must implement `ICorrelatable`.
- There is no distributed transaction. Classify each step as compensable, pivot or retryable, write undo commands for
  the compensable ones, and put the pivot as late as possible.
- Only re-send the original command after a timeout if that command is idempotent.
- Saga state updates run under optimistic concurrency. Two replies arriving at once means one retries, which is another
  reason side effects belong in the declaration.

## Default implementation shape

1. State: `SagaStateBase` subclass, `required` initiating data, mutable transition fields, pure helper methods.
2. Saga: `Saga<TState>` with state name constants and a `Configure` method covering initial, waiting, fault, timeout
   and final states.
3. Persistence: `AddPostgresSagas()` in both the `DbContext` model and the bus builder, plus the migration.
4. Scheduling: a store that supports the timeout.
5. Tests: happy path to the final response, fault path to compensation, timeout path, and a restart mid-process.

## Verification

```bash
dotnet build
dotnet ef migrations list
dotnet test --filter "FullyQualifiedName~Saga"
```

`MO0014` means the saga has a constructor parameter. Inspect the registered state machine with
`IMessagingRuntime.Description.Sagas`, where every state carries its transitions and an `IsFinal` flag. A waiting state
whose transitions lead nowhere final is visible there before production finds it for you.

## References

- [Sagas](https://chillicream.com/docs/mocha/sagas)
- [Scheduling](https://chillicream.com/docs/mocha/scheduling)
- [mocha-best-practices: sagas](../../mocha-best-practices/references/sagas.md)
- [mocha-best-practices: scheduling](../../mocha-best-practices/references/scheduling.md)
