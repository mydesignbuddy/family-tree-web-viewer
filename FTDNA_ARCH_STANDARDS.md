# FTDNA Architectural Standards Guide

> A reference guide for building and maintaining Domain-Driven Design solutions at FTDNA, derived from the DataAudit service implementation.

---

## Table of Contents

1. [Guiding Principles](#guiding-principles)
2. [Solution Structure](#solution-structure)
3. [Layer Responsibilities](#layer-responsibilities)
4. [Dependency Rules](#dependency-rules)
5. [Technical Stack](#technical-stack)
6. [Messaging & CQRS](#messaging--cqrs)
7. [Domain Modeling](#domain-modeling)
8. [Data Access Patterns](#data-access-patterns)
9. [Configuration Management](#configuration-management)
10. [Dependency Injection & Composition Root](#dependency-injection--composition-root)
11. [Error Handling](#error-handling)
12. [Infrastructure & Deployment](#infrastructure--deployment)
13. [Naming Conventions](#naming-conventions)
14. [Decision Log](#decision-log)

---

## Guiding Principles

1. **Clean separation of concerns** — Each project has a single reason to change.
2. **Domain at the center** — Business logic lives in the Domain layer, free of infrastructure concerns.
3. **Dependency inversion** — Outer layers depend on inner layers, never the reverse.
4. **Convention over configuration** — Prefer framework conventions (e.g., Wolverine handler discovery) over explicit registration.
5. **Standalone solutions** — New bounded contexts are isolated solutions to prevent accidental coupling with existing services.
6. **No unnecessary abstractions** — Only abstract when there is a concrete need for substitution or testability.

---

## Solution Structure

Every new service should follow this five-project layout:

```
{ServiceName}/
├── FTDNA.Services.{ServiceName}.Common/        # Shared config & interfaces
├── FTDNA.Services.{ServiceName}.Domain/         # Business logic & orchestration
├── FTDNA.Services.{ServiceName}.Data.Internal/  # FTDNA data access
├── FTDNA.Services.{ServiceName}.Data.External/  # Third-party data access
├── FTDNA.Services.{ServiceName}.Worker/         # Background service host (optional)
│   ├── Program.cs
│   ├── Dockerfile
│   ├── appsettings.json
│   └── appsettings.{Environment}.json
├── FTDNA.Services.{ServiceName}.Api/            # REST API host (optional)
│   ├── Program.cs
│   ├── Dockerfile
│   ├── appsettings.json
│   └── appsettings.{Environment}.json
├── FTDNA.Services.{ServiceName}.Web/            # Web UI host (optional)
│   ├── Program.cs
│   ├── Dockerfile
│   ├── appsettings.json
│   └── appsettings.{Environment}.json
├── {ServiceName}.sln
├── nuget.config
├── task-definition-{service-name}.json
├── ARCHITECTURE.md
└── README.md
```

### Host Projects (Worker, Api, Web)

At least one host project is required. A solution may have multiple:

- **Worker** — Background services, scheduled tasks, queue consumers. Uses `IHostedService` / `BackgroundService`. Base image: `dotnet/runtime`.
- **Api** — REST API endpoints. Uses controllers or minimal APIs. Base image: `dotnet/aspnet`.
- **Web** — Web UI (Razor Pages, Blazor, etc.). Base image: `dotnet/aspnet`.

All host projects follow the same rules: they reference Domain and Common only, never Data projects directly. Each has its own `Program.cs`, `Dockerfile`, and appsettings files.

### When to split Data.Internal vs Data.External

- **Data.Internal** — Accesses databases and services owned by FTDNA (e.g., FTDNA SQL Server).
- **Data.External** — Accesses databases and services owned by partner organizations or third parties (e.g., FIGG PostgreSQL via SSH tunnel).
- If the service only talks to FTDNA systems, a single `Data` project is acceptable.

---

## Layer Responsibilities

### Common

- Configuration POCOs (e.g., `AwsConfig`, `SshTunnelConfig`)
- Configuration factories
- Cross-cutting interfaces that decouple outer layers (e.g., `ISshTunnelStatus`)
- **No business logic. No data access. No framework dependencies.**

### Domain

- Commands and their handlers (business orchestration)
- Domain models and enums
- Mapping configuration (Mapster `TypeAdapterConfig`)
- Service registration entry point (`Startup.cs`) that composes all layers
- References both Data projects and Common

### Data.Internal / Data.External

- Query records and their handlers (data retrieval only)
- Data-layer model records (internal representations)
- Infrastructure services (e.g., `SshTunnelService`)
- Service registration (`Startup.cs`)
- References Common only — **never references Domain or the other Data project**

### Worker (or API)

- Host configuration (`Program.cs`)
- Background workers or API controllers
- Dockerfile and deployment manifests
- References Common and Domain only — **never references Data projects directly**

---

## Dependency Rules

```
Worker ──→ Domain ──→ Data.Internal ──→ Common
  │           │                            ▲
  │           └──→ Data.External ──────────┘
  └──────────→ Common
```

**Strict rules:**

| Project | May Reference |
|---------|--------------|
| Common | Nothing (root) |
| Data.Internal | Common |
| Data.External | Common |
| Domain | Data.Internal, Data.External, Common |
| Worker / Api / Web | Domain, Common |

Host projects (Worker, Api, Web) **must not** reference Data projects. All data access is mediated through the Domain layer via the message bus.

---

## Technical Stack

### Core Libraries

| Library | Purpose | Replaces |
|---------|---------|----------|
| **WolverineFx** | Message bus, mediator, handler discovery, and async messaging (SQS/SNS/RabbitMQ) | MediatR (license changed to paid), AWSSDK.SQS / RabbitMQ.Client (direct SDK usage) |
| **Mapster** | Object mapping between layers | AutoMapper (license changed to paid) |
| **FluentResults** | Typed Result/error propagation | Exceptions for flow control |
| **Entity Framework Core** | Primary ORM for data access (SQL Server, PostgreSQL) | — |
| **Microsoft.Data.SqlClient / Npgsql** | Raw ADO.NET for high-volume bulk reads where ORM overhead is not justified | EF Core (when performance allows) |
| **Amazon.Extensions.Configuration.SystemsManager** | AWS Parameter Store config | Manual secrets management |
| **AWS.Logger.AspNetCore** | CloudWatch structured logging | File-based logging |

### Framework

- **.NET 8.0** (LTS) — all new services target the current LTS release.
- **Docker** — multi-stage Linux builds for all deployable services.

---

## Messaging & CQRS

We use **Wolverine** as a lightweight mediator to separate _what_ from _how_:

### Async Messaging (SQS/SNS)

Wolverine has built-in support for AWS SQS, SNS, and RabbitMQ transports. **Prefer Wolverine's transport integration over direct SDK usage** (AWSSDK.SQS, RabbitMQ.Client, etc.) for queue publishing and consumption. This keeps all message dispatch — both in-process and async — flowing through a single abstraction.

> **Note:** The DataAudit service predates this standard and uses AWSSDK.SQS directly. New services should use Wolverine's SQS transport instead.

### Commands (Domain Layer)

Commands represent business operations. They are dispatched by the Worker and handled in the Domain.

```csharp
// Domain/Audit/OptOutCompliance/Command.cs
public record OptOutComplianceCommand;

// Domain/Audit/OptOutCompliance/Handler.cs
public class OptOutComplianceHandler
{
    public async Task<Result<AuditResult>> Handle(
        OptOutComplianceCommand command,
        IMessageBus bus,
        DataAuditConfig config,
        ILogger<OptOutComplianceHandler> logger)
    {
        // Orchestrate data queries via bus, apply business rules
    }
}
```

### Queries (Data Layer)

Queries represent data retrieval. They live in the Data projects and are dispatched by Domain handlers.

```csharp
// Data.Internal/KitOptState/GetOptedInPage/Query.cs
public record GetOptedInPageQuery(int LastGrcId, int PageSize);

// Data.Internal/KitOptState/GetOptedInPage/Handler.cs
public class GetOptedInPageHandler
{
    public async Task<List<FtdnaKitRecord>> Handle(
        GetOptedInPageQuery query,
        FtdnaDbConfig config)
    {
        // Raw SQL, return data-layer models
    }
}
```

### Conventions

- **One folder per operation** containing `Command.cs`/`Query.cs` and `Handler.cs`.
- Handler classes use Wolverine's convention: a public `Handle` method whose first parameter is the message type.
- No marker interfaces on messages — they are plain `record` types.
- Dependencies are injected as method parameters (Wolverine's cascading IoC), not constructor parameters.

### Handler Discovery

Register handler assemblies in the Domain `Startup`:

```csharp
public static void ConfigureWolverine(WolverineOptions options)
{
    options.Discovery.IncludeAssembly(typeof(Data.Internal.Startup).Assembly);
    options.Discovery.IncludeAssembly(typeof(Data.External.Startup).Assembly);
    options.Discovery.IncludeAssembly(typeof(Domain.Startup).Assembly);
}
```

---

## Domain Modeling

### Use Records for Models

All models are immutable `record` types. Separate domain models from data-layer models — map between them with Mapster.

```csharp
// Domain model
public record FtdnaOptedInKit(Guid ForensicKitNum, int GrcId);

// Data-layer model (Data.Internal)
public record FtdnaKitRecord(Guid ForensicKitNum, int GrcId);
```

Even when the shapes are identical, maintaining separate records per layer ensures each layer can evolve independently.

### Enums for Classification

Use enums for domain-specific classifications:

```csharp
public enum DiscrepancyType
{
    OptOutNotSynced,
    OptInNotSynced,
    OptInMissing
}
```

### Aggregate Results

Bundle audit/operation results into a single domain model:

```csharp
public record AuditResult(
    string AuditName,
    DateTime RunAt,
    int TotalChecked,
    List<AuditDiscrepancy> Discrepancies);
```

---

## Data Access Patterns

### Entity Framework Core (Default)

EF Core is the standard ORM for most services. It provides developer productivity, change tracking, migrations, and query composition.

**When to drop to raw ADO.NET:** High-volume bulk read operations (millions of rows) where ORM overhead is measurable. The DataAudit service is an example — it processes 2M+ rows using raw SqlClient/Npgsql with keyset pagination for maximum throughput.

```csharp
// Raw ADO.NET — use only when EF Core performance is insufficient
await using var connection = new SqlConnection(config.ConnectionString);
await connection.OpenAsync();
await using var cmd = new SqlCommand(sql, connection);
cmd.Parameters.AddWithValue("@lastGrcId", query.LastGrcId);
cmd.Parameters.AddWithValue("@pageSize", query.PageSize);
```

### Pagination Strategy

- **Keyset (cursor) pagination** — always preferred over OFFSET/FETCH for large datasets.
- Use the last record's key from the previous page as the cursor for the next page.
- SQL Server: `WHERE Id > @cursor ORDER BY Id`
- PostgreSQL: `WHERE id > @cursor ORDER BY id`

### Batch Lookups

When cross-referencing between databases, use batch queries with parameterized IN clauses:

```csharp
// Build dynamic parameters: @p0, @p1, @p2, ...
var paramNames = ids.Select((_, i) => $"@p{i}").ToList();
var sql = $"SELECT ... WHERE ForensicKitNum IN ({string.Join(",", paramNames)})";
```

PostgreSQL alternative using array parameters:

```csharp
cmd.Parameters.AddWithValue("@ids", ids.ToArray());
// SQL: WHERE forensic_kit_num = ANY(@ids)
```

### Configurable Batch Size

All batch operations use a configurable batch size (default: 500) stored in `DataAuditConfig.BatchSize`. This allows tuning per environment without code changes.

---

## Configuration Management

### Layered Configuration Sources

Priority order (highest wins):

1. **Environment variables**
2. **AWS Parameter Store** (refreshed on interval, e.g., every 5 minutes)
3. **appsettings.{Environment}.json**
4. **appsettings.json**

### Configuration Factory Pattern

Centralize config construction in a factory that reads from `IConfiguration` and produces a single typed config object:

```csharp
public static class DataAuditConfigFactory
{
    public static DataAuditConfig Build(IConfiguration configuration)
    {
        // Map IConfiguration sections to strongly-typed config
    }
}
```

### Environment Profiles

- `Development` — local dev, console logging, debug verbosity
- `gbg-figg-dev` — deployed dev environment
- `gbg-figg-prod` — production

Set via `DOTNET_ENVIRONMENT` environment variable.

### Secrets

- **Never commit secrets.** Use AWS Parameter Store or mounted files.
- SSH keys: prefer `PrivateKeyPath` (mounted PEM) in production; `PrivateKeyContent` (Parameter Store) as fallback.
- Database credentials: stored in Parameter Store, injected via config.

---

## Dependency Injection & Composition Root

### Registration Flow

Each layer exposes a `Startup.ConfigureServices(IServiceCollection, DataAuditConfig)` method. The Domain layer's Startup is the composition root that chains all registrations:

```csharp
// Domain/Startup.cs
public static class Startup
{
    public static void ConfigureServices(IServiceCollection services, DataAuditConfig config)
    {
        DataInternal.Startup.ConfigureServices(services, config);
        DataExternal.Startup.ConfigureServices(services, config);

        // Domain-level registrations (mapping, etc.)
        var mapConfig = new TypeAdapterConfig();
        mapConfig.Scan(typeof(Startup).Assembly);
        services.AddSingleton(mapConfig);
    }

    public static void ConfigureWolverine(WolverineOptions options)
    {
        options.Discovery.IncludeAssembly(typeof(DataInternal.Startup).Assembly);
        options.Discovery.IncludeAssembly(typeof(DataExternal.Startup).Assembly);
        options.Discovery.IncludeAssembly(typeof(Startup).Assembly);
    }
}
```

### Worker Program.cs

The entry point is minimal — it builds the host, loads config, and delegates to Domain:

```csharp
var builder = Host.CreateApplicationBuilder(args);

// Load AWS Parameter Store config
builder.Configuration.AddSystemsManager("/ftdna-forensic-data-audit",
    options => options.ReloadAfter = TimeSpan.FromMinutes(5));

var config = DataAuditConfigFactory.Build(builder.Configuration);
Startup.ConfigureServices(builder.Services, config);

builder.Host.UseWolverine(options => Startup.ConfigureWolverine(options));
builder.Services.AddHostedService<DataAuditWorker>();
```

### Interface-Based Decoupling

When the Worker needs to interact with an infrastructure service owned by a Data project, define the interface in Common:

```csharp
// Common/Interfaces/ISshTunnelStatus.cs
public interface ISshTunnelStatus
{
    bool IsConnected { get; }
}

// Data.External/Services/SshTunnelService.cs — implements ISshTunnelStatus
// Worker/Workers/DataAuditWorker.cs — injects ISshTunnelStatus
```

This preserves the dependency rule: Worker depends on Common, not Data.External.

---

## Error Handling

### FluentResults

All handlers return `Result<T>` from the FluentResults library. This replaces exception-based flow control with explicit success/failure paths.

```csharp
public async Task<Result<AuditResult>> Handle(...)
{
    var queryResult = await bus.InvokeAsync<List<FtdnaKitRecord>>(query);

    if (queryResult is null)
        return Result.Fail("Failed to retrieve kit records");

    // ... business logic ...

    return Result.Ok(auditResult);
}
```

**Rules:**

- Never throw exceptions for expected business conditions.
- Propagate failures up the call chain; let the Worker/API decide how to handle them.
- Log at the point of failure with sufficient context.

### Logging Conventions

| Level | Use For |
|-------|---------|
| Debug | Diagnostics only needed during development |
| Information | Audit lifecycle events (start, complete, counts) |
| Warning | Data discrepancies, degraded conditions, dev-only fallbacks |
| Error | Handler failures, connectivity failures, unrecoverable issues |

Always include structured data (kit numbers, counts, batch IDs) in log messages for CloudWatch queryability.

---

## Infrastructure & Deployment

### Docker

Multi-stage builds for minimal image size:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
# Restore, build, publish

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
# Copy published output, set entrypoint
```

- Use `runtime` base (not `aspnet`) for worker services without HTTP.
- Use `aspnet` base for API services.
- Pass NuGet credentials as build args for private feed access.

### AWS ECS Fargate

- **CPU/Memory:** Start with 512 CPU / 1024 MB; scale based on workload.
- **Logging:** awslogs driver → CloudWatch log group per environment.
- **Networking:** awsvpc mode for VPC-native networking.
- **Roles:** Separate task role (runtime permissions) and execution role (pull/start permissions).
- **Config:** `DOTNET_ENVIRONMENT` environment variable selects the appsettings profile.

### Task Definition

Maintain a `task-definition-{service-name}.json` in the solution root. This is the source of truth for ECS deployment configuration.

---

## Naming Conventions

### Projects

```
FTDNA.Services.{ServiceName}.{Layer}
```

### Folders

- Group by **feature/aggregate**, not by technical role.
- Data operations: `{Aggregate}/{OperationName}/Query.cs` + `Handler.cs`
- Domain operations: `{Aggregate}/{OperationName}/Command.cs` + `Handler.cs`

### Files

| Type | Convention | Example |
|------|-----------|---------|
| Command | `Command.cs` (in feature folder) | `Audit/OptOutCompliance/Command.cs` |
| Query | `Query.cs` (in feature folder) | `KitOptState/GetOptedInPage/Query.cs` |
| Handler | `Handler.cs` (in feature folder) | `KitOptState/GetOptedInPage/Handler.cs` |
| Domain Model | `{Name}.cs` in `Models/` | `Models/AuditResult.cs` |
| Data Model | `{Name}.cs` in `Models/` | `Models/FtdnaKitRecord.cs` |
| Config POCO | `{Name}Config.cs` in `Config/` | `Config/AwsConfig.cs` |
| Service Registration | `Startup.cs` at project root | `Startup.cs` |

### Records & Classes

- Domain models: named after the ubiquitous language (`AuditDiscrepancy`, `FtdnaOptedInKit`)
- Data models: suffixed with `Record` (`FtdnaKitRecord`, `FiggKitRecord`)
- Config objects: suffixed with `Config` (`DataAuditConfig`, `SshTunnelConfig`)
- Workers: suffixed with `Worker` (`DataAuditWorker`)
- Services: suffixed with `Service` (`SshTunnelService`)

---

## Decision Log

Architectural decisions and their rationale, to be referenced when evaluating alternatives for new services.

### Wolverine over MediatR

**Decision:** Use WolverineFx as the message bus / mediator.
**Rationale:** MediatR changed to a paid license. Wolverine provides equivalent handler dispatch with convention-based discovery, method-parameter DI injection, and no marker interfaces on messages. It also offers a path to full async messaging if needed later.

### Mapster over AutoMapper

**Decision:** Use Mapster for object mapping.
**Rationale:** AutoMapper changed to a paid license. Mapster provides equivalent mapping capabilities with compile-time code generation support and lower allocation overhead.

### Raw ADO.NET over EF Core

**Decision:** Use raw SQL with SqlClient/Npgsql for data access.
**Rationale:** The DataAudit service performs bulk read operations across millions of rows. ORM overhead (change tracking, materialization) is unnecessary and harmful at this scale. Raw ADO.NET allows precise control over pagination strategy (keyset cursors) and query optimization.

### Keyset Pagination over OFFSET

**Decision:** Use cursor/keyset pagination for all large dataset traversal.
**Rationale:** OFFSET-based pagination degrades linearly with page depth. Keyset pagination (`WHERE id > @cursor`) maintains constant performance regardless of dataset position. Critical for processing 2M+ row datasets efficiently.

### Standalone Solution over Shared Project

**Decision:** Each bounded context is its own .sln with its own deployment artifact.
**Rationale:** Prevents accidental coupling between services. Enables independent build, test, and deploy cycles. Clear ownership boundaries. The minor cost of duplicated boilerplate is offset by operational independence.

### FluentResults over Exceptions

**Decision:** Use `Result<T>` return types for all handler operations.
**Rationale:** Exceptions are expensive and obscure control flow. FluentResults makes success/failure explicit in the type signature, forces callers to handle both paths, and enables clean error aggregation without try/catch nesting.

### Background Worker over Scheduled Lambda

**Decision:** Long-running ECS Fargate task over Lambda invocations.
**Rationale:** Audit cycles process millions of rows over 3-5 minutes with persistent database connections and SSH tunnels. Lambda's 15-minute timeout, cold starts, and connection overhead make it unsuitable. ECS Fargate provides stable, long-running compute with the same serverless operational model.

---

## Checklist for New Services

When creating a new service, verify:

- [ ] Solution follows the five-project structure (or justified subset)
- [ ] Dependency rules are enforced — no upward or lateral references
- [ ] All models are immutable `record` types
- [ ] Domain models are separate from data-layer models with explicit mapping
- [ ] Commands/Queries use folder-per-operation layout
- [ ] Wolverine handles all cross-layer dispatch
- [ ] Configuration uses the factory pattern with Parameter Store integration
- [ ] FluentResults used for all handler return types
- [ ] No secrets in source control — Parameter Store or mounted files only
- [ ] Dockerfile uses multi-stage build with appropriate base image
- [ ] ECS task definition committed to solution root
- [ ] ARCHITECTURE.md documents the service's specific design
- [ ] README.md covers purpose, prerequisites, and local development
