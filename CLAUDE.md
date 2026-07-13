# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project overview

Núcleo — a full-stack IT asset management platform for SMBs: one IT service provider attends multiple client companies, each with assets (hardware/software/network equipment), service tickets, and a monthly retainer contract. Technicians (system users) work tickets and change asset states.

Currently backend-only (`backend/Nucleo.Api`); an Angular 19 (standalone components + signals) frontend is planned but not started. The project is being built incrementally, one phase at a time, with each phase explained (especially EF Core relationship config and DI wiring) and validated against live endpoints before moving to the next — preserve this workflow rather than generating everything at once.

## Commands

Run from repo root (`Nucleo.slnx` is the solution file, in the new .slnx XML format).

```
# Build
dotnet build backend/Nucleo.Api/Nucleo.Api.csproj

# Run (dev) — serves http://localhost:5112 (Swagger UI at /swagger); HTTPS profile is localhost:7010
dotnet run --project backend/Nucleo.Api/Nucleo.Api.csproj --launch-profile http
```

On startup in `Development`, `Program.cs` automatically runs `Database.MigrateAsync()` and `DbSeeder.SeedAsync()` — no manual migration step needed for local dev after pulling changes.

Migrations (requires the `dotnet-ef` global tool: `dotnet tool install --global dotnet-ef`):
```
dotnet ef migrations add <Name> --project backend/Nucleo.Api/Nucleo.Api.csproj --startup-project backend/Nucleo.Api/Nucleo.Api.csproj -o Data/Migrations
dotnet ef database update --project backend/Nucleo.Api/Nucleo.Api.csproj --startup-project backend/Nucleo.Api/Nucleo.Api.csproj
```

No test project exists yet.

**Stack notes (deviations from a generic ASP.NET tutorial, worth knowing before assuming otherwise):**
- Target framework is **net10.0** (not net9) — that's what was available on the dev machine; EF Core packages are pinned to `10.0.9` to match.
- Database is **SQL Server Express**, instance `.\SQLEXPRESS` (not LocalDB) — connection string lives in `appsettings.json` under `ConnectionStrings:NucleoDb`, Windows auth, database name `NucleoDb`.
- **JWT signing key is NOT in `appsettings.json`** — only `Jwt:Issuer`/`Jwt:Audience`/`Jwt:ExpiresInMinutes` live there. The actual `Jwt:Key` is in `dotnet user-secrets` (run `dotnet user-secrets set "Jwt:Key" "<value>"` from `backend/Nucleo.Api`); `Program.cs` throws a clear startup error if it's missing. Seeded técnicos all share the demo password `Nucleo123!` (see `DbSeeder.PasswordDemoTecnicos`) — emails: `carlos.mendez@nucleo.mx` (Tecnico), `sofia.ramirez@nucleo.mx` (Lector), `diego.torres@nucleo.mx` (Admin).

## Architecture

The layered architecture is the core exercise of this project, not an incidental convention — keep it strict:

**Controller → Service → Repository → DbContext**

- **Controllers** (`Controllers/`) only translate HTTP ⇄ service calls. No EF Core, no business logic.
- **Services** (`Services/`) hold all business rules: validation, conflict checks, cross-repository orchestration, transactions, and entity↔DTO mapping.
- **Repositories** (`Repositories/`) are the only layer that touches `AppDbContext`/EF Core. No business logic.
- The generic `IRepositorio<T>` is registered as an **open generic** (`AddScoped(typeof(IRepositorio<>), typeof(Repositorio<>))` in `Program.cs`), so it resolves for any entity without per-entity registration. Specific repositories (`IClienteRepositorio`, `IActivoRepositorio`, `IHistorialActivoRepositorio`) layer on domain-specific queries (uniqueness checks, `Include`-based joins) on top of the generic CRUD.
- Repository write methods (`Agregar`/`Actualizar`/`Eliminar`) **do not call `SaveChanges`** — they only stage changes on the shared, `Scoped` `AppDbContext`. Callers (Services) explicitly call `GuardarCambiosAsync()` to persist. This is deliberate, not an oversight: it's what makes explicit transaction handling actually necessary (see below) rather than cosmetic.
- DTOs (`Models/DTOs/`) are always distinct from entities (`Models/Entities/`); controllers/services never expose entities directly over HTTP.
- Domain exceptions (`Common/Exceptions/`) drive HTTP status codes via a global `IExceptionHandler` (`Common/GlobalExceptionHandler.cs`) that maps to `ProblemDetails`: `RecursoNoEncontradoException` → 404, `ConflictoException` → 409, anything else → 500 with no internal detail leaked.

### Transactional state changes (the key exercise)

Changing an `Activo`'s `Estado` must write an audit row to `HistorialActivo` in the **same transaction** — if the audit write fails, the state change rolls back. Because the Service touches two repositories (`IActivoRepositorio` + `IHistorialActivoRepositorio`) sharing one `Scoped` `AppDbContext`, and each repo's `GuardarCambiosAsync()` is a separate `SaveChanges` call, the two writes are **not** atomic together by default — two sequential `SaveChangesAsync()` calls are two separate implicit transactions. This is solved with an explicit `IUnitOfWork` (`Repositories/IUnitOfWork.cs` / `UnitOfWork.cs`) that wraps EF Core's `IDbContextTransaction` without leaking EF types into the Service layer:

`IniciarTransaccionAsync()` → update `Activo` + `GuardarCambiosAsync()` → insert `HistorialActivo` + `GuardarCambiosAsync()` → `ConfirmarTransaccionAsync()`, with `RevertirTransaccionAsync()` on any failure in between.

See `ActivoService.CambiarEstadoAsync` for the full pattern — treat it as the reference implementation for any future multi-repository transactional operation (e.g. Ticket state changes).

Valid `Activo` state transitions are defined once in `Domain/EstadoActivoTransiciones.cs` (a static lookup table), consulted by the service **before** opening a transaction. `Retirado` is a terminal state with no outgoing transitions.

`TecnicoId` existence for a state change is intentionally **not** pre-validated in C# — it's enforced by the SQL Server FK constraint on `HistorialActivo.TecnicoId`. `ActivoService` catches the resulting `DbUpdateException` (SQL error 547), translates it to a 404 `RecursoNoEncontradoException`, and rolls back. Since Fase 3, `tecnicoId` is no longer client-supplied (it's read from the JWT's `NameIdentifier` claim in `ActivosController.CambiarEstado`, not from `CambiarEstadoActivoDto`), so this FK-violation path is now a defensive backstop rather than the primary way to trigger it — reproducing the rollback deliberately requires deleting/tampering the técnico row directly (e.g. via `sqlcmd`) while a still-valid token references it.

### Auth (Fase 3)

JWT bearer auth, roles `Admin`/`Tecnico`/`Lector` (`Models/Entities/Enums.cs` → `RolTecnico`, string constants in `Common/Roles.cs`). `AuthController.Login` → `AuthService` (BCrypt.Verify against `Tecnico.PasswordHash`) → `ITokenService.GenerarToken` (claims: `NameIdentifier`=id, `Email`, `Name`, `Role`). `[Authorize]` at controller level on `ClientesController`/`ActivosController` (any authenticated técnico can read); `[Authorize(Roles = Roles.Escritura)]` on POST/PUT/DELETE/PATCH actions (`Lector` is read-only). `AuthController` itself has no `[Authorize]` — login must stay anonymous. Login failure (unknown email OR wrong password) throws `CredencialesInvalidasException` → 401, with the same generic message either way (doesn't leak whether the email exists).

### EF Core model conventions

- Entity configuration lives in `Data/Configurations/`, one `IEntityTypeConfiguration<T>` per entity, loaded via `modelBuilder.ApplyConfigurationsFromAssembly` in `AppDbContext.OnModelCreating`.
- Enums are stored as `nvarchar(20)` (`HasConversion<string>()`) for DB readability, **and** serialized as strings over the API (`JsonStringEnumConverter` registered globally in `Program.cs`) — both DB rows and JSON payloads use names like `"EnReparacion"`, never raw ints.
- Delete behaviors are deliberately mixed to avoid SQL Server's "multiple cascade paths" restriction: `Activo → HistorialActivo` is the only `Cascade`; `Tecnico → HistorialActivo` and `Cliente → (Activo/Ticket/Contrato)` are `Restrict`; the nullable `Ticket → Activo` FK is `SetNull`. When adding new FK relationships, check for cascade-path conflicts before defaulting to `Cascade`.
- `DbSeeder.cs` seeds `Clientes` and `Tecnicos` **independently and idempotently** (each gated on its own `AnyAsync` check, not a single top-level check) — follow this pattern when adding new seed data so it isn't silently skipped just because some other table already has rows.

## Project state

- **Fase 1 (done):** Cliente CRUD, base EF model for all 6 entities, generic repository, DI wiring, global exception handling.
- **Fase 2 (done):** Activo CRUD, state machine, transactional state-change + audit trail, historial endpoint.
- **Fase 3 (done):** JWT auth + BCrypt, role-based `[Authorize]` on Cliente/Activo endpoints.
- **Planned:** Ticket/Contrato CRUD + JOIN-heavy reports and aggregations (Fase 4), Angular 19 frontend (Fase 5-6). See `C:\Users\domin\Downloads\proyecto-nucleo-spec (1).md` for the full phase list — this project follows that spec, reconciled against choices made before it was shared (see project memory).
