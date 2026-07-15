# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project overview

Núcleo — a full-stack IT asset management platform for SMBs: one IT service provider attends multiple client companies, each with assets (hardware/software/network equipment), service tickets, and a monthly retainer contract. Technicians (system users) work tickets and change asset states.

Backend at `backend/Nucleo.Api`; frontend at `frontend/` (Angular 19, standalone components + signals). The project is being built incrementally, one phase at a time, with each phase explained (especially EF Core relationship config, DI wiring, and Angular's standalone/signals patterns) and validated live (Postman for the API, the actual browser for the frontend) before moving to the next — preserve this workflow rather than generating everything at once.

## Commands

Run from repo root (`Nucleo.slnx` is the solution file, in the new .slnx XML format).

```
# Build
dotnet build backend/Nucleo.Api/Nucleo.Api.csproj

# Run (dev) — serves http://localhost:5112 (Swagger UI at /swagger); HTTPS profile is localhost:7010
dotnet run --project backend/Nucleo.Api/Nucleo.Api.csproj --launch-profile http
```

```
# Frontend — serves http://localhost:4200 (proxies nothing; calls the API directly, CORS-enabled for this origin)
npm start --prefix frontend
# or: cd frontend && npm start   (wraps `ng serve`)
```

Both must run simultaneously for the frontend to work. `frontend/src/environments/environment.ts` hardcodes `apiUrl: 'http://localhost:5112/api'` (no prod/dev file-replacement split set up — not needed, nothing is deployed).

On startup in `Development`, `Program.cs` automatically runs `Database.MigrateAsync()` and `DbSeeder.SeedAsync()` — no manual migration step needed for local dev after pulling changes.

Migrations (requires the `dotnet-ef` global tool: `dotnet tool install --global dotnet-ef`):
```
dotnet ef migrations add <Name> --project backend/Nucleo.Api/Nucleo.Api.csproj --startup-project backend/Nucleo.Api/Nucleo.Api.csproj -o Data/Migrations
dotnet ef database update --project backend/Nucleo.Api/Nucleo.Api.csproj --startup-project backend/Nucleo.Api/Nucleo.Api.csproj
```

```
# Tests (61 pruebas: Services con Moq + máquinas de estados) — la API NO debe estar corriendo (bloquea el .exe)
dotnet test backend/Nucleo.Api.Tests/Nucleo.Api.Tests.csproj
```

Tests cover the Service layer with mocked repositories (no DB) — see `docs/TESTING.md` for the full coverage map, the manual per-phase validation record, and the `SqlExceptionFactory` reflection helper used to fabricate SQL error 547 for the FK-rollback test.

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

`TecnicoId` existence for a state change is intentionally **not** pre-validated in C# — it's enforced by the SQL Server FK constraint on `HistorialActivo.TecnicoId`. `ActivoService` catches the resulting `DbUpdateException` (SQL error 547), translates it to a 404 `RecursoNoEncontradoException`, and rolls back. Since Fase 3, `tecnicoId` is no longer client-supplied (it's read from the JWT's `sub` claim in `ActivosController.CambiarEstado`, not from `CambiarEstadoActivoDto`), so this FK-violation path is now a defensive backstop rather than the primary way to trigger it — reproducing the rollback deliberately requires deleting/tampering the técnico row directly (e.g. via `sqlcmd`) while a still-valid token references it.

### Auth (Fase 3)

JWT bearer auth, roles `Admin`/`Tecnico`/`Lector` (`Models/Entities/Enums.cs` → `RolTecnico`, string constants in `Common/Roles.cs`). `AuthController.Login` → `AuthService` (BCrypt.Verify against `Tecnico.PasswordHash`) → `ITokenService.GenerarToken`. `[Authorize]` at controller level on every domain controller (any authenticated técnico can read); `[Authorize(Roles = Roles.Escritura)]` on POST/PUT/DELETE/PATCH actions (`Lector` is read-only). `AuthController` itself has no `[Authorize]` — login must stay anonymous. Login failure (unknown email OR wrong password) throws `CredencialesInvalidasException` → 401, with the same generic message either way (doesn't leak whether the email exists).

**JWT claims are short, non-standard names** (`sub`, `email`, `name`, `role`), not `ClaimTypes.*` (which serialize as long WS-Federation URIs like `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/role`) — chosen so the Angular frontend can decode the token payload and read `payload.role` directly instead of an ugly URI key. Two things had to be configured together to make this work, both easy to break if touched in isolation:
1. `TokenService.GenerarToken` emits `new Claim(JwtRegisteredClaimNames.Sub, ...)` / `new Claim("role", ...)`, not `ClaimTypes.*`.
2. `Program.cs`'s `AddJwtBearer` sets `options.MapInboundClaims = false` **and** `TokenValidationParameters.NameClaimType = JwtRegisteredClaimNames.Sub` / `RoleClaimType = "role"`. Without `MapInboundClaims = false`, `JwtBearerHandler` silently remaps `sub`→`ClaimTypes.NameIdentifier` and `role`→`ClaimTypes.Role`'s WS-Federation URI **on every incoming request**, regardless of what the token actually contains — this breaks `[Authorize(Roles = ...)]` everywhere (403 on all role-restricted endpoints) with no useful error message. If role-based authorization mysteriously stops working after touching JWT config, check this first.

CORS is enabled (`AddCors`/`UseCors("Frontend")` in `Program.cs`) for `http://localhost:4200` only — required because the Angular dev server and the API run on different ports (different origins). This only breaks in real browsers (preflight `OPTIONS` requests); it's invisible to Postman/`Invoke-RestMethod`, which don't enforce same-origin policy — always sanity-check auth flows in an actual browser, not just an HTTP client.

### Tickets and reporting (Fase 4)

`Ticket` state machine (`Domain/EstadoTicketTransiciones.cs`): `Abierto → EnProgreso → Resuelto → Cerrado`, plus an escape hatch `Abierto → Cancelado`; both `Cerrado` and `Cancelado` are terminal and auto-set `FechaCierre`. Unlike `Activo`, there's no audit table for tickets, so `TicketService.CambiarEstadoAsync` is a single `SaveChanges` — no `IUnitOfWork` needed. This asymmetry is intentional and worth pointing out when extending either flow: only add explicit transaction handling when an operation truly spans more than one repository/table.

`TicketService.CrearAsync`/`ActualizarAsync` enforce a cross-entity business rule beyond FK existence: if `ActivoId` is supplied, that activo must belong to the same `ClienteId` as the ticket (`ConflictoException` → 409 otherwise).

`GET /api/tickets` satisfies the spec's "reporte con JOINs" requirement directly — `TicketRepositorio` triple-`Include`s Cliente/Activo/Tecnico so `TicketResponseDto` carries resolved names, not raw FK ids. No separate report endpoint was added for this; it would have been redundant.

`GET /api/reportes/dashboard` (`ReportesController` → `IReporteService` → `IReporteRepositorio`) is the aggregation exercise: `GROUP BY` (activos por estado, tickets por estado/prioridad), `SUM` (ingresos mensuales recurrentes de contratos activos), `AVG` (horas incluidas promedio), and a subquery/anti-join (clientes activos sin ningún ticket Abierto/EnProgreso). `IReporteRepositorio` is the one repository that doesn't wrap a single entity — it queries `AppDbContext` directly since it necessarily crosses Cliente/Activo/Ticket/Contrato, but it's still the only layer touching EF Core, consistent with the rest of the architecture. `ReporteService` awaits each repository call **sequentially, not via `Task.WhenAll`** — all calls share one `Scoped` `AppDbContext`, and EF Core does not support concurrent operations on the same context instance (this would throw at runtime, not just be inefficient).

### EF Core model conventions

- Entity configuration lives in `Data/Configurations/`, one `IEntityTypeConfiguration<T>` per entity, loaded via `modelBuilder.ApplyConfigurationsFromAssembly` in `AppDbContext.OnModelCreating`.
- Enums are stored as `nvarchar(20)` (`HasConversion<string>()`) for DB readability, **and** serialized as strings over the API (`JsonStringEnumConverter` registered globally in `Program.cs`) — both DB rows and JSON payloads use names like `"EnReparacion"`, never raw ints.
- Delete behaviors are deliberately mixed to avoid SQL Server's "multiple cascade paths" restriction: `Activo → HistorialActivo` is the only `Cascade`; `Tecnico → HistorialActivo` and `Cliente → (Activo/Ticket/Contrato)` are `Restrict`; the nullable `Ticket → Activo` FK is `SetNull`. When adding new FK relationships, check for cascade-path conflicts before defaulting to `Cascade`.
- `DbSeeder.cs` seeds `Clientes` and `Tecnicos` **independently and idempotently** (each gated on its own `AnyAsync` check, not a single top-level check) — follow this pattern when adding new seed data so it isn't silently skipped just because some other table already has rows.

### Frontend (Fase 5-6)

Standalone Angular 19, no NgModules, no SSR. Structure under `frontend/src/app/`:
- `core/services/auth.service.ts` — signal-based session state (`usuario` readonly signal; `estaAutenticado`/`puedeEscribir`/`esAdmin` computed). On construction, rehydrates the session from the JWT already in `localStorage` (`core/utils/jwt.util.ts` decodes the payload client-side, no signature check — that's the backend's job on every request) instead of calling the API again on page reload.
- `core/services/nucleo-api.service.ts` — single HTTP client for the whole domain API; returns raw Observables so each component decides async pipe (lists) vs subscribe (one-off actions).
- `core/models/nucleo.models.ts` — TS mirrors of the backend DTOs/enums, **including the two state-transition tables** (`TRANSICIONES_ACTIVO`/`TRANSICIONES_TICKET`). The frontend uses them only to render valid options; the backend remains the real validator (409 otherwise).
- `core/interceptors/auth.interceptor.ts` — a functional `HttpInterceptorFn` that attaches `Authorization: Bearer <token>` **only** to requests whose URL starts with `environment.apiUrl` (never leaks the token to third-party requests).
- `core/guards/auth.guard.ts` (`CanActivateFn`) and `core/guards/role.guard.ts` (a **factory** — `roleGuard(['Admin'])` — not a bare guard, since it needs a parameter). Both redirect via `router.createUrlTree(...)` rather than injecting `Router.navigate` imperatively.
- `features/*` are lazy-loaded standalone components via `loadComponent` in `app.routes.ts` (not `loadChildren`/NgModules) — each feature lands in its own chunk.

Where each spec concept deliberately lives (Fase 6):
- **async pipe**: `features/clientes` list (`clientes$ | async`); its mutations use imperative subscribe — the async-vs-subscribe contrast is intentional.
- **effect**: `features/activos` — the cliente filter is a signal and an `effect` in the constructor reloads the list when it changes; the `<select>` only writes the signal. Note: mutations call an explicit `recargar()` because re-setting the same filter value doesn't retrigger the effect.
- **computed**: `features/tickets` board — columns and per-column/global counters all derive from one `tickets` signal; mutations only `update()` that signal and everything re-renders (validated live: counters moved without reload). Also `features/dashboard`, which computes GROUP BY distributions client-side from raw lists, while financial metrics (ingresos/promedio/clientes sin tickets) still come from `GET /api/reportes/dashboard` since they cross tables the front doesn't load.
- **Role-based UI**: write forms/buttons render only if `auth.puedeEscribir()`; the Admin nav link only if `esAdmin()`. This is UX only — the API's 403 remains the enforcement.

`GET /api/tecnicos` (`TecnicosController`, read-only, added in Fase 6) exists solely to populate the ticket-assignment selector; `TecnicoResponseDto` never exposes `PasswordHash`.

Run both servers together to develop against this (`dotnet run` + `npm start --prefix frontend`); a `.claude/launch.json` entry named `frontend` exists for the Browser-pane tooling.

## Project state

- **Fase 1 (done):** Cliente CRUD, base EF model for all 6 entities, generic repository, DI wiring, global exception handling.
- **Fase 2 (done):** Activo CRUD, state machine, transactional state-change + audit trail, historial endpoint.
- **Fase 3 (done):** JWT auth + BCrypt, role-based `[Authorize]` on all domain endpoints.
- **Fase 4 (done):** Ticket CRUD + state machine, JOIN-based ticket listing, dashboard aggregations. Contrato/Ticket seed data added (both tables existed since the Fase 1 migration but were never seeded until now).
- **Fase 5 (done):** Angular 19 project structure, JWT interceptor, `authGuard`/`roleGuard`, login screen. Validated end-to-end in a real browser (not just Postman) — this is what surfaced the CORS and `MapInboundClaims` issues documented above.
- **Fase 6 (done):** real feature UIs — Clientes (async pipe), Activos (effect-driven filter, state change + audit trail), Tickets kanban board and dashboard (computed signals), role-aware UI. All 6 planned phases complete; remaining ideas are the spec's stretch goals (SLA alerts, server-side pagination, SignalR, exports).
- See `C:\Users\domin\Downloads\proyecto-nucleo-spec (1).md` for the original spec — this project follows it, reconciled against choices made before it was shared (see project memory).
