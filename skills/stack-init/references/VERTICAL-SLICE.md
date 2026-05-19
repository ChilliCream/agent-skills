# Vertical Slice Architecture reference

Organize code by feature, not by layer. Each feature is a self-contained slice that owns its request, handler, validator, response, persistence calls, and endpoint wiring. Cross-cutting concerns live in shared infrastructure; everything else lives in its slice.

The rationale: most changes touch one feature top-to-bottom. Horizontal layering (`Controllers/`, `Services/`, `Repositories/`) forces every change to scatter across folders. Vertical slicing keeps the change colocated.

## Philosophy

Pick Vertical Slice when the fastest safe path is one feature folder per behavior. It is agent-friendly because the next change usually copies the nearest slice instead of coordinating four projects.

## Folder layout

```
src/
  Features/
    CreateBook/
      CreateBookCommand.cs          # request
      CreateBookHandler.cs          # handler (MediatR or minimal)
      CreateBookValidator.cs        # FluentValidation
      CreateBookResponse.cs         # response DTO
      CreateBookEndpoint.cs         # minimal API / controller / GraphQL field
    GetBookById/
      GetBookByIdQuery.cs
      GetBookByIdHandler.cs
      GetBookByIdEndpoint.cs
    RenameAuthor/
      ...
  Shared/
    Persistence/
      AppDbContext.cs
    Behaviors/
      ValidationBehavior.cs
      LoggingBehavior.cs
  Program.cs
```

No `Controllers/`, `Services/`, `Repositories/` folders at the top level. Every feature is a folder, every folder is a slice.

## What lives in a slice

A typical slice contains:

- **Request** — the input DTO. Often a MediatR `IRequest<TResponse>`.
- **Handler** — the use case logic. Talks directly to the DbContext or whatever shared infrastructure it needs.
- **Validator** — input validation (FluentValidation runs as a MediatR pipeline behavior).
- **Response** — output DTO.
- **Endpoint** — the transport wiring. A minimal API `app.MapPost(...)`, a controller action, or a GraphQL mutation that calls `mediator.Send(request)`.

Slices can have their own private types — DTOs, helpers — that no other slice needs.

## CQRS within slices

Commands (writes) and queries (reads) are separate slices, even if they touch the same entity. `CreateBook`, `RenameBook`, `DeleteBook`, `GetBookById`, `ListBooks` are five folders. They share the entity definition but nothing else.

This makes it explicit which operations mutate and which do not — no helper that "looks like a read but writes a log row" sneaking past review.

## Sharing

Resist the urge to extract "common" handler code. A small duplication beats a wrong abstraction. Extract only when:

- The duplicated code is **infrastructure**, not domain (auth check, logging, transaction wrapping → put in a MediatR pipeline behavior).
- Three or more slices have the **exact same** logic and the rule is stable.

If "shared" turns into a service that every slice calls, you have rebuilt horizontal layering inside the slice structure.

## Cross-cutting concerns

Put them in pipeline behaviors, not in handlers:

```csharp
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        => _validators = validators;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var context = new ValidationContext<TRequest>(request);
        var failures = (await Task.WhenAll(_validators.Select(v => v.ValidateAsync(context, ct))))
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();
        if (failures.Count > 0) throw new ValidationException(failures);
        return await next();
    }
}
```

Register the behavior once; every command/query routes through it.

## When to pick Vertical Slice

- The system is feature-rich but the features are independent. A SaaS admin panel, a workflow engine.
- The team is uncomfortable with the indirection of Clean Architecture and finds it hard to remember which layer a piece of code belongs to.
- You expect features to be added and removed frequently — slices delete cleanly.

Avoid when:

- You have a rich domain with complex invariants spanning many entities. The Domain layer of Clean Architecture / DDD earns its keep there.
- You have many transports for the same core logic (REST + GraphQL + CLI + bus) — slices duplicate the endpoint wiring per feature.

## Common mistakes

- **Slices that import each other.** If `CreateBook` references `RenameBook`, the slicing has failed. Extract the shared piece into Shared/ or rethink the boundary.
- **A `Services/` folder appearing in `Shared/`.** Means the team has reintroduced horizontal layering. Push the logic back into the slice that needs it.
- **One slice per HTTP route.** A route is not a feature — `GET /books` and `GET /books/{id}` are arguably the same slice. Group by behaviour, not URL.

## Agentic coding preparation

`ARCHITECTURE.md` should tell agents:

- Start every change by finding or creating the feature folder.
- Keep request, handler, validator, endpoint/resolver, and response together.
- Put only stable infrastructure in `Shared/`.
- Never make slices depend on other slices.

`DOMAIN.md` can be lighter than in Clean Architecture, but it still needs entity names, invariants, and glossary terms so slices do not diverge in language.

## Reference project

Use `assets/reference-projects/vertical-slice/` as the starter shape. Rename namespaces and feature names before copying.
