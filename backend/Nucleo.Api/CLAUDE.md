# Nucleo.Api

ASP.NET Core Web API. Layers: `Controllers/` → `Services/` → `Repositories/` → `Data/AppDbContext` — each layer folder has its own CLAUDE.md with its rules.

## Stack notes (deviations from a generic ASP.NET tutorial, worth knowing before assuming otherwise)

- Target framework is **net10.0** (not net9) — that's what was available on the dev machine; EF Core packages are pinned to `10.0.9` to match.
- Database is **SQL Server Express**, instance `.\SQLEXPRESS` (not LocalDB) — connection string lives in `appsettings.json` under `ConnectionStrings:NucleoDb`, Windows auth, database name `NucleoDb`.
- **JWT signing key is NOT in `appsettings.json`** — only `Jwt:Issuer`/`Jwt:Audience`/`Jwt:ExpiresInMinutes` live there. The actual `Jwt:Key` is in `dotnet user-secrets` (run `dotnet user-secrets set "Jwt:Key" "<value>"` from `backend/Nucleo.Api`); `Program.cs` throws a clear startup error if it's missing. Seeded técnicos all share the demo password `Nucleo123!` (see `DbSeeder.PasswordDemoTecnicos`).

## Program.cs wiring

- The generic `IRepositorio<T>` is registered as an **open generic** (`AddScoped(typeof(IRepositorio<>), typeof(Repositorio<>))`), so it resolves for any entity without per-entity registration.
- `JsonStringEnumConverter` is registered globally — enums travel as names (`"EnReparacion"`), never raw ints (they're also stored as strings in the DB, see `Data/CLAUDE.md`).
- In `Development`, startup runs `Database.MigrateAsync()` + `DbSeeder.SeedAsync()`. `args[0] == "seed-bulk"` short-circuits into `CargaSeeder` instead of starting the host.

### Auth (Fase 3)

JWT bearer auth, roles `Admin`/`Tecnico`/`Lector` (`Models/Entities/Enums.cs` → `RolTecnico`, string constants in `Common/Roles.cs`). `AuthController.Login` → `AuthService` (BCrypt.Verify against `Tecnico.PasswordHash`) → `ITokenService.GenerarToken`. Login failure (unknown email OR wrong password) throws `CredencialesInvalidasException` → 401, with the same generic message either way (doesn't leak whether the email exists). Authorization attributes: see `Controllers/CLAUDE.md`.

**JWT claims are short, non-standard names** (`sub`, `email`, `name`, `role`), not `ClaimTypes.*` (which serialize as long WS-Federation URIs like `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/role`) — chosen so the Angular frontend can decode the token payload and read `payload.role` directly instead of an ugly URI key (`frontend/src/app/core/utils/jwt.util.ts` depends on this). Two things had to be configured together to make this work, both easy to break if touched in isolation:
1. `TokenService.GenerarToken` emits `new Claim(JwtRegisteredClaimNames.Sub, ...)` / `new Claim("role", ...)`, not `ClaimTypes.*`.
2. `Program.cs`'s `AddJwtBearer` sets `options.MapInboundClaims = false` **and** `TokenValidationParameters.NameClaimType = JwtRegisteredClaimNames.Sub` / `RoleClaimType = "role"`. Without `MapInboundClaims = false`, `JwtBearerHandler` silently remaps `sub`→`ClaimTypes.NameIdentifier` and `role`→`ClaimTypes.Role`'s WS-Federation URI **on every incoming request**, regardless of what the token actually contains — this breaks `[Authorize(Roles = ...)]` everywhere (403 on all role-restricted endpoints) with no useful error message. If role-based authorization mysteriously stops working after touching JWT config, check this first.

CORS is enabled (`AddCors`/`UseCors("Frontend")`) for `http://localhost:4200` only — required because the Angular dev server and the API run on different ports (different origins). This only breaks in real browsers (preflight `OPTIONS` requests); it's invisible to Postman/`Invoke-RestMethod`, which don't enforce same-origin policy — always sanity-check auth flows in an actual browser, not just an HTTP client.

## Common/ and Models/

- Domain exceptions (`Common/Exceptions/`) drive HTTP status codes via a global `IExceptionHandler` (`Common/GlobalExceptionHandler.cs`) that maps to `ProblemDetails`: `RecursoNoEncontradoException` → 404, `ConflictoException` → 409, `CredencialesInvalidasException` → 401, anything else → 500 with no internal detail leaked. Services signal errors by throwing these, never by returning status codes.
- DTOs (`Models/DTOs/`) are always distinct from entities (`Models/Entities/`); controllers/services never expose entities directly over HTTP. `TecnicoResponseDto` never exposes `PasswordHash`. The frontend mirrors these DTOs in `frontend/src/app/core/models/nucleo.models.ts` — change both together.
- `ResultadoPaginadoDto<T>` — `{ items, pagina, tamanoPagina, totalRegistros, hayMas }` — is the list-endpoint contract for Clientes, Activos and Tickets. `TotalRegistros` is the total **of the applied filter**, not of the table (the kanban relies on this for per-column counters).
