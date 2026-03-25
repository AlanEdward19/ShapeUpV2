en# AGENTS.md - ShapeUp API Guidance

## Project Overview
**ShapeUp** is an ASP.NET Core 10 Web API built using **Vertical Slice Pattern** with **Domain-Driven Design (DDD)** principles. The architecture is **monolayer** where features are separated by domain boundaries. All code follows **SOLID principles** and is designed for **testability**.

## Tech Stack & Key Files

| Component | Version | File |
|-----------|---------|------|
| .NET SDK | 10.0 | `ShapeUp.csproj` |
| ASP.NET Core | 10.0.4 | `Program.cs` |
| OpenAPI/Swagger | 10.0.4 | Built-in |

**Key Dependencies:**
- `Microsoft.AspNetCore.OpenApi` - Integrated OpenAPI support (no separate Swagger needed)
- `Microsoft.EntityFrameworkCore` - ORM for data access
- `FluentValidation` - Mandatory validation library for commands/queries
- Additional .NET 10 standard libraries as needed

## Architecture Patterns

### Vertical Slice Pattern
- Features are organized as **vertical slices**, each containing all layers needed for a complete feature
- Each slice is independent and can be developed, tested, and deployed separately
- Slices contain: API endpoints, business logic, data access, and DTOs in a cohesive unit

### Domain-Driven Design (DDD)
- Code is organized by **business domains**, not technical layers
- Each domain represents a bounded context with clear responsibilities
- Example directory structure:
  ```
  Features/
  ├── UserManagement/
  │   ├── CreateUser/
  │   ├── UpdateUser/
  │   └── DeleteUser/
  ├── Orders/
  │   ├── PlaceOrder/
  │   ├── CancelOrder/
  │   └── GetOrder/
  ```

### File-Scoped Namespaces
All files use modern file-scoped namespace syntax:
```csharp
namespace ShapeUp.Features.UserManagement.CreateUser;
```
Namespaces follow the directory structure exactly—this is mandatory for this project.

### Nullable Reference Types
`<Nullable>enable</Nullable>` enforces strict null-safety. All string properties must be `string?` unless guaranteed non-null.

### Result Pattern (Mandatory)
- **Always use ResultPattern** (`Result` / `Result<T>`) for domain/business outcomes.
- **Do not throw exceptions for controlled domain errors** (validation, not found, forbidden, conflict, etc.).
- Domain errors must be represented by structured errors (code + message + status code) and returned through ResultPattern.
- **Only use exceptions for unexpected/uncontrolled failures** (infrastructure, runtime, transient faults).
- Controllers must map `Result` to HTTP responses (including non-2xx like `403`) using shared mapping extensions.

### Pagination (Mandatory)
- **All read endpoints returning collections must use keyset pagination**.
- **Offset pagination (`Skip/Take` with page number) is not allowed** for collection endpoints.
- Cursor must be opaque (e.g., base64) and based on stable indexed ordering keys.
- Responses must return both `items` and `nextCursor`.
- Repositories must apply pagination at query level (database), never in-memory.

### CQRS (Mandatory)
- **All features must follow CQRS**: separate write operations (`Command`) from read operations (`Query`).
- Write flows must use command request/handler pairs; read flows must use query request/handler pairs.
- Controllers must only orchestrate transport concerns and delegate to CQRS handlers.

### CancellationToken (Mandatory)
- **All endpoints must accept `CancellationToken cancellationToken`**.
- The token must be propagated to validators, handlers, repositories, and EF Core calls.
- Do not create `CancellationToken.None` in request flow unless there is an explicit technical reason.

### FluentValidation (Mandatory)
- **All validation must use FluentValidation** (`AbstractValidator<T>` + `IValidator<T>`).
- Validators are required for both commands and queries when input constraints exist.
- Validation execution must be async with `ValidateAsync(..., cancellationToken)`.

### Domain Architecture Documentation (Mandatory)
- **Every implemented domain must include an architecture file inside the domain directory**: `Features/{Domain}/ARCHITECTURE.md`.
- This file must document domain scope, database structure (when applicable), endpoints, and end-to-end flow.
- The document must end with an **ASCII diagram** following the same style used in `Features/Authorization/`.
- Keep this file updated whenever the domain changes.

## SOLID Principles

### Single Responsibility Principle (SRP)
- Each class has one reason to change
- Example: `CreateUserCommandHandler` only handles user creation logic
- Example: `UserRepository` only handles data persistence

### Open/Closed Principle (OCP)
- Classes are open for extension, closed for modification
- Use abstraction (interfaces) for extensibility
- Example: `IValidator<CreateUserCommand>` allows rule extension without modifying handlers

### Liskov Substitution Principle (LSP)
- Derived classes must be substitutable for base classes
- All implementations of `IHandler<TRequest, TResponse>` must work identically from caller perspective

### Interface Segregation Principle (ISP)
- Clients should depend on specific interfaces, not general ones
- Example: `IRepository` is split into `IReadRepository<T>` and `IWriteRepository<T>`

### Dependency Inversion Principle (DIP)
- Depend on abstractions, not concrete implementations
- Constructor injection of interfaces only
- All dependencies registered in `Program.cs`

## Testability & Dependency Injection

### Constructor Injection Pattern
```csharp
public class CreateUserCommandHandler
{
    private readonly IUserRepository _repository;
    private readonly IValidator<CreateUserCommand> _validator;

    // Inject dependencies via constructor
    public CreateUserCommandHandler(IUserRepository repository, IValidator<CreateUserCommand> validator)
    {
        _repository = repository;
        _validator = validator;
    }
}
```

### Service Registration in Program.cs
```csharp
// Register interfaces, not concrete implementations
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<CreateUserCommandHandler>();
builder.Services.AddScoped<IValidator<CreateUserCommand>, CreateUserCommandValidator>();
```

### Testing Considerations
- Use interfaces for all external dependencies
- Mock `IRepository`, `IValidator<T>`, etc. in unit tests
- No static methods (breaks testability)
- No file I/O without abstraction

## Directory Structure

```
ShapeUp/
├── Program.cs                      # Application startup & DI configuration
├── Features/                       # Vertical slices by domain
│   ├── UserManagement/
│   │   ├── ARCHITECTURE.md         # Domain architecture (db, endpoints, flow, ASCII diagram)
│   │   ├── CreateUser/
│   │   │   ├── CreateUserCommand.cs
│   │   │   ├── CreateUserResponse.cs
│   │   │   ├── CreateUserCommandHandler.cs
│   │   │   ├── CreateUserCommandValidator.cs
│   │   │   └── CreateUserController.cs
│   │   ├── UpdateUser/
│   │   └── DeleteUser/
│   └── Orders/
│       ├── ARCHITECTURE.md         # Domain architecture (db, endpoints, flow, ASCII diagram)
│       ├── PlaceOrder/
│       ├── CancelOrder/
│       └── GetOrder/
├── Shared/                         # Cross-cutting concerns
│   ├── Abstractions/               # Interfaces
│   │   ├── IRepository.cs
│   │   └── IHandler.cs
│   ├── Results/                    # ResultPattern contracts + HTTP mapping
│   │   ├── Result.cs
│   │   ├── Error.cs
│   │   ├── CommonErrors.cs
│   │   └── ResultControllerExtensions.cs
│   └── Extensions/                 # Extension methods
├── Properties/
│   └── launchSettings.json
└── ShapeUp.csproj
```

## Creating a New Feature

### 1. Define Request/Response DTOs
Create `Features/{Domain}/{Feature}/{FeatureName}Request.cs` and `Response.cs`:
```csharp
namespace ShapeUp.Features.UserManagement.CreateUser;

public record CreateUserCommand(string Name, string Email);
public record CreateUserResponse(int Id, string Name, string Email);
```

### 2. Create Handler/Business Logic
Create `Features/{Domain}/{Feature}/{FeatureName}Handler.cs`:
```csharp
namespace ShapeUp.Features.UserManagement.CreateUser;

public class CreateUserCommandHandler
{
    private readonly IUserRepository _repository;
    private readonly IValidator<CreateUserCommand> _validator;

    public CreateUserCommandHandler(IUserRepository repository, IValidator<CreateUserCommand> validator)
    {
        _repository = repository;
        _validator = validator;
    }

    public async Task<Result<CreateUserResponse>> HandleAsync(
        CreateUserCommand command,
        CancellationToken cancellationToken)
    {
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
            return Result<CreateUserResponse>.Failure(
                CommonErrors.Validation(string.Join("; ", validation.Errors.Select(x => x.ErrorMessage))));

        var user = new User { Name = command.Name, Email = command.Email };
        await _repository.AddAsync(user, cancellationToken);
        
        return Result<CreateUserResponse>.Success(new CreateUserResponse(user.Id, user.Name, user.Email));
    }
}
```

### 3. Create Validator (FluentValidation)
Create `Features/{Domain}/{Feature}/{FeatureName}Validator.cs`:
```csharp
namespace ShapeUp.Features.UserManagement.CreateUser;

using FluentValidation;

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}
```

### 4. Create Controller/Endpoint
Create `Features/{Domain}/{Feature}/{FeatureName}Controller.cs`:
```csharp
namespace ShapeUp.Features.UserManagement.CreateUser;

[ApiController]
[Route("api/users")]
public class CreateUserController : ControllerBase
{
    private readonly CreateUserCommandHandler _handler;

    public CreateUserController(CreateUserCommandHandler handler)
    {
        _handler = handler;
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateUserCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _handler.HandleAsync(command, cancellationToken);
        return this.ToActionResult(
            result,
            success => CreatedAtAction(nameof(Create), success));
    }
}
```

### 5. Register in Program.cs
```csharp
// Register handler and dependencies
builder.Services.AddScoped<CreateUserCommandHandler>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IValidator<CreateUserCommand>, CreateUserCommandValidator>();
```

## Build & Run Commands

```bash
# Build
dotnet build

# Run HTTP endpoint (port 5141)
dotnet run --launch-profile http

# Run HTTPS endpoint (port 7020)
dotnet run --launch-profile https

# Run tests
dotnet test
```

## Code Style Conventions

- **Implicit usings**: Enabled—common namespaces auto-imported
- **Nullable references**: All new code must respect strict null-safety
- **Target framework**: .NET 10—use latest features (records, init-only properties, collection expressions)
- **Naming**: Namespaces match directory structure exactly
- **Records over classes**: Use `record` for DTOs/immutable data
- **No static methods**: All behavior goes through interfaces for testability
- **Explicit dependencies**: Constructor injection only, no service locator patterns

## .NET 10 Modern Features & Best Practices

Leverage all modern .NET 10 capabilities for cleaner, more efficient code:

### Language Features
- **Records**: Immutable DTOs with automatic `Equals`, `GetHashCode`, and `ToString`
  ```csharp
  public record CreateUserCommand(string Name, string Email);
  ```
- **Init-only properties**: For immutable data after initialization
  ```csharp
  public class User { public string Name { get; init; } }
  ```
- **Collection Expressions**: Use `[item1, item2]` instead of `new List<T> { ... }`
  ```csharp
  var errors = ["Name is required", "Email is required"];
  ```
- **Primary Constructors**: Generate field assignments automatically (C# 12)
  ```csharp
  public class CreateUserCommandHandler(IUserRepository repo, IValidator<CreateUserCommand> validator)
  {
      // Auto-generated: private readonly IUserRepository repo; etc.
  }
  ```
- **Pattern Matching**: Use switch expressions for elegant control flow
  ```csharp
  var statusCode = result.Status switch
  {
      ValidationStatus.Valid => 200,
      ValidationStatus.Invalid => 400,
      _ => 500
  };
  ```
- **Nullable Reference Types with `required` keyword**: Force non-null initialization
  ```csharp
  public class User { public required string Name { get; init; } }
  ```

### Async & Performance
- **ValueTask over Task** for small synchronous results (reduces allocations)
- **IAsyncEnumerable<T>** for streaming large datasets efficiently
- **Span<T> and Memory<T>** for stack allocation and zero-copy scenarios
- **using declarations** (file-scoped): Automatically dispose resources at end of scope
  ```csharp
  using var reader = new StreamReader("file.txt");
  ```

### EF Core Data Access
```csharp
// DbContext configuration in Program.cs
builder.Services
    .AddDbContext<ShapeUpDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
```

**Entity Configuration:**
- Use **Entity Framework Core conventions** for minimal configuration
- Implement `IEntityTypeConfiguration<T>` in `Features/{Domain}/Data/` for fluent API configuration
- Use `.HasKey()`, `.HasMany()`, `.WithOne()` for relationships
- Enable **QueryFilters** for soft deletes (partition by domain)

**Query Optimization:**
- Use `.AsNoTracking()` for read-only queries (EF doesn't track changes)
- Use `.Select()` projection to fetch only needed columns
- Use `.Include()` judiciously—prefer lazy loading or explicit queries
- Batch operations with `.ExecuteUpdate()` / `.ExecuteDelete()` (EF Core 7+)

## Performance & Computational Efficiency

Always prioritize efficient, low-cost computation:

### General Principles
- **Early termination**: Return early with ResultPattern to avoid unnecessary processing
  ```csharp
  if (!validation.IsValid)
      return Result<MyResponse>.Failure(CommonErrors.Validation("Validation failed"));
  // Continue with expensive operations only if valid
  ```
- **Lazy evaluation**: Use LINQ deferred execution—avoid `.ToList()` prematurely
- **Caching**: Use in-memory caching for read-heavy, slow-changing data
  - `IMemoryCache` for local caching
  - Consider distributed caching for scaled scenarios
- **Async all the way**: Prevent blocking thread pool (crucial for scalability)

### Memory Optimization
- **Avoid unnecessary allocations**: Prefer `ref struct` for internal processing
- **String efficiency**: Use `string.Intern()` for frequently compared strings (sparingly)
- **Collection pooling**: Use `ArrayPool<T>` for temporary buffers in hot paths
  ```csharp
  using var buffer = ArrayPool<byte>.Shared.Rent(size);
  try { /* use buffer */ } finally { ArrayPool<byte>.Shared.Return(buffer); }
  ```

### Database Optimization
- **Batch operations**: Group updates/inserts instead of individual calls
  ```csharp
  await context.Users.AddRangeAsync(users);
  await context.SaveChangesAsync(); // Single database roundtrip
  ```
- **Pagination**: Always paginate large result sets (no `.ToList()` on 1M rows)
  ```csharp
  var page = await users.Skip((pageNum - 1) * pageSize).Take(pageSize).ToListAsync();
  ```
- **Indexes**: Document required database indexes in entity configuration comments
- **Keyset indexes**: Ensure sort/filter columns used by keyset cursor are indexed
- **EF Core compiled queries**: Pre-compile frequently-used LINQ queries
  ```csharp
  private static readonly Func<ShapeUpDbContext, int, Task<User?>> GetUserById = 
      EF.CompileAsyncQuery((ShapeUpDbContext ctx, int id) => ctx.Users.FirstOrDefault(u => u.Id == id));
  ```

### API Response Efficiency
- **DTO projection**: Only serialize necessary fields to minimize payload
- **Compression**: ASP.NET Core handles gzip automatically, ensure enabled
- **Streaming responses**: Use `IAsyncEnumerable<T>` for large exports
  ```csharp
  [HttpGet("export")]
  public async IAsyncEnumerable<UserDto> ExportUsers()
  {
      await foreach (var user in _repository.GetAllAsync()) 
          yield return MapToDto(user);
  }
  ```

## Common Tasks for AI Agents

### Adding a New Feature
1. Create feature directory: `Features/{Domain}/{Feature}/`
2. Create command/query + response records (CQRS)
3. Create command/query handler with business logic
4. Create FluentValidation validator(s) for command/query
5. Create controller endpoint with `CancellationToken` and delegate to handler
6. For list endpoints, implement **keyset pagination** (`cursor` + `pageSize`)
7. Create or update `Features/{Domain}/ARCHITECTURE.md` with database structure (if any), endpoints, flow, and an ASCII diagram at the end (as in `Authorization`)
8. Register dependencies in `Program.cs` (including FluentValidation)

### Database Operations with EF Core
- Inject `DbContext` via constructor into repositories
- Use `.AsNoTracking()` for read-only queries (performance)
- Use `.Select()` to project only needed fields
- Use `.ExecuteUpdate()` / `.ExecuteDelete()` for bulk operations (no tracking overhead)
- Always call `await context.SaveChangesAsync()` after modifications
- Example repository pattern:
  ```csharp
  public class UserRepository : IUserRepository
  {
      private readonly ShapeUpDbContext _context;
      
      public UserRepository(ShapeUpDbContext context) => _context = context;
      
      public async Task<User?> GetByIdAsync(int id) =>
          await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);
      
      public async Task AddAsync(User user)
      {
          _context.Users.Add(user);
          await _context.SaveChangesAsync();
      }
  }
  ```

### Adding Cross-Domain Logic
- Place shared abstractions in `Shared/Abstractions/`
- Create shared result/error contracts in `Shared/Results/`
- Use extension methods in `Shared/Extensions/` for utility functions

### Unit Testing
- Mock all `IRepository`, `IValidator<T>` dependencies
- Test handler in isolation
- Test FluentValidation rules separately
- Use `xunit` or `nunit` with `Moq` for mocking

---
**Last Updated:** 2026-03-24  
**Target Framework:** .NET 10.0  
**Architecture:** Vertical Slice Pattern + Domain-Driven Design  
**Design Principles:** SOLID + Testability First

