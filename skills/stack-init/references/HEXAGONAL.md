# Hexagonal Architecture reference

Also known as Ports & Adapters. The application core sits in the middle; every interaction with the outside world goes through a **port** (an interface declared by the core) and is fulfilled by an **adapter** (an implementation that lives outside the core).

The shape is a hexagon because there are no top/bottom or front/back layers — every external boundary is equal.

## Philosophy

Pick Hexagonal when the core must survive adapter churn. It gives agents one durable rule: the core declares what it needs through ports; adapters translate technology into those ports.

## Core and adapters

### Core (inside)

Pure application code. Knows nothing about HTTP, databases, queues, or files. Contains:

- **Domain model** — entities, value objects, domain events (same shape as in Clean Architecture / DDD).
- **Use cases / application services** — orchestrate domain operations. Equivalent to MediatR command handlers in Clean Architecture.
- **Ports** — interfaces the core declares to describe what it needs from the outside. Two flavours:
  - **Driving ports** (input) — what the core lets external callers do. `ICreateBook`, `IRenameAuthor`.
  - **Driven ports** (output) — what the core needs from outside. `IBookRepository`, `IEmailSender`, `IClock`.

### Adapters (outside)

Implementations of ports. Contains:

- **Driving adapters** — translate external input into calls on driving ports. A REST controller, a GraphQL resolver, a CLI command, a message bus consumer. They depend on the core.
- **Driven adapters** — implement driven ports using concrete tech. `EfBookRepository : IBookRepository`, `SmtpEmailSender : IEmailSender`. They depend on the core.

## Dependency rule

```
[driving adapter] ──► [driving port] ──► [core] ──► [driven port] ◄── [driven adapter]
        (REST/GraphQL)                                 (Repository)        (EF Core)
```

All dependency arrows point at the core. The core never references an adapter; adapters reference the core.

## Folder layout

```
src/
  Core/                            # the hexagon
    Domain/
      Book.cs
      Author.cs
    Application/
      CreateBookUseCase.cs
    Ports/
      In/                          # driving
        ICreateBook.cs
      Out/                         # driven
        IBookRepository.cs
        IClock.cs
  Adapters/
    Web/                           # driving adapter
      BooksController.cs
    Persistence/                   # driven adapter
      EfBookRepository.cs
      AppDbContext.cs
    Time/
      SystemClock.cs
  Host/                            # composition root
    Program.cs                     # wires adapters to ports via DI
```

Adapter projects depend on Core. Core depends on nothing in the solution.

## Worked example — creating a book

1. **Driving adapter** (REST controller) receives `POST /books`. It maps the JSON body to a `CreateBookInput` and calls `ICreateBook.HandleAsync(input)`.

2. **Core** — `CreateBookUseCase : ICreateBook` validates input, calls `Book.Create(...)`, persists via the `IBookRepository` port:

   ```csharp
   public sealed class CreateBookUseCase(IBookRepository books, IClock clock) : ICreateBook
   {
       public async Task<Book> HandleAsync(CreateBookInput input, CancellationToken ct)
       {
           var book = Book.Create(input.AuthorId, input.CreatedBy, input.Title, clock.UtcNow);
           await books.AddAsync(book, ct);
           return book;
       }
   }
   ```

3. **Driven adapter** — `EfBookRepository : IBookRepository` does the EF Core call. The core never sees `DbContext`.

## When to pick Hexagonal over Clean Architecture

They overlap heavily. Use Hexagonal when:

- You want to emphasize testability of the core in isolation — every dependency is a port, every port has an in-memory test double.
- The system has many distinct adapters (REST + GraphQL + message bus + CLI all driving the same core).
- The team thinks in terms of "what does the core need" rather than "what layer does this live in".

Use Clean Architecture when you want a clearer recipe for where files live (Domain/Application/Infrastructure/Presentation projects). The shapes converge in practice.

## Common mistakes

- **Ports that are just thin wrappers over the adapter.** `IEmailSender.SendAsync(SmtpMessage)` leaks SMTP into the core. Define the port in domain terms — `IEmailSender.SendAsync(string to, string subject, string body)` — and let the adapter translate.
- **Core referencing adapters.** A `using Microsoft.EntityFrameworkCore;` in the core layer is the first sign the architecture has decayed. Block it with a `.csproj` reference graph that compiles only one way.
- **God ports.** `IRepository<T>` with twenty methods. Split by use case — the core should depend on `IFindBookById`, not `IBookRepository.FindAll/FindByTitle/FindByAuthor/...`.

## Agentic coding preparation

`ARCHITECTURE.md` should tell agents:

- Add use cases and ports in Core.
- Add technology-specific code only in Adapters.
- Wire adapters in Host/composition root.
- Never pass adapter-specific types through core ports.

`DOMAIN.md` should name the core model in technology-free terms so ports stay domain-shaped.

## Reference project

Use `assets/reference-projects/hexagonal/` as the starter shape. Rename namespaces and adapters before copying.
