# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project overview

Núcleo — a full-stack IT asset management platform for SMBs: one IT service provider attends multiple client companies, each with assets (hardware/software/network equipment), service tickets, and a monthly retainer contract. Technicians (system users) work tickets and change asset states.

Backend at `backend/Nucleo.Api` (ASP.NET Core, net10.0, EF Core + SQL Server Express); frontend at `frontend/` (Angular 19, standalone components + signals). The project is being built incrementally, one phase at a time, with each phase explained (especially EF Core relationship config, DI wiring, and Angular's standalone/signals patterns) and validated live (Postman for the API, the actual browser for the frontend) before moving to the next — preserve this workflow rather than generating everything at once.

**Layered architecture is the core exercise, keep it strict: Controller → Service → Repository → DbContext.** Only Repositories touch EF Core; only Services hold business rules. Details in the per-layer CLAUDE.md files below.

## Mapa de CLAUDE.md (se cargan al tocar archivos de esa carpeta)

| Archivo | Cubre |
|---|---|
| `backend/Nucleo.Api/CLAUDE.md` | stack, `Program.cs` (DI, JWT, CORS, JSON), `Common/`, `Models/` |
| `backend/Nucleo.Api/Controllers/CLAUDE.md` | HTTP-only, `[Authorize]`/roles, `tecnicoId` desde `sub` |
| `backend/Nucleo.Api/Services/CLAUDE.md` | reglas de negocio, UnitOfWork/transacciones, paginación, SLA, reportes |
| `backend/Nucleo.Api/Repositories/CLAUDE.md` | único acceso a EF, sin `SaveChanges`, `IReporteRepositorio` |
| `backend/Nucleo.Api/Data/CLAUDE.md` | configuraciones EF, cascadas, seeders, datos de carga, índices |
| `backend/Nucleo.Api/Domain/CLAUDE.md` | máquinas de estados (espejadas en el frontend) |
| `backend/Nucleo.Api.Tests/CLAUDE.md` | pruebas con Moq, `SqlExceptionFactory` |
| `frontend/CLAUDE.md` | calidad (axe, Lighthouse, responsive), environment |
| `frontend/src/app/core/CLAUDE.md` | auth signals, interceptor, guards, modelos espejo |
| `frontend/src/app/features/CLAUDE.md` | qué patrón Angular vive en qué feature (a propósito) |
| `frontend/src/app/features/tickets/CLAUDE.md` | kanban paginado por columna, alertas de SLA |

**Skills del proyecto** (`.claude/skills/`): `/nueva-migracion <Nombre>` (revisar modelo → generar → revisar el `.cs` → aplicar y verificar con `sqlcmd`) y `/validar-fase <cambio>` (API con los 3 roles + navegador real + axe/overflow → registro en `docs/TESTING.md`). Úsalas en vez de improvisar esos flujos.

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

Both must run simultaneously for the frontend to work; `.claude/launch.json` has `api` and `frontend` entries for the Browser-pane tooling (`preview_start {name: "api"}` / `{name: "frontend"}`) — use those instead of starting servers from a shell. On startup in `Development`, `Program.cs` automatically runs `Database.MigrateAsync()` and `DbSeeder.SeedAsync()` — no manual migration step needed for local dev after pulling changes.

Migrations (requires the `dotnet-ef` global tool: `dotnet tool install --global dotnet-ef`):
```
dotnet ef migrations add <Name> --project backend/Nucleo.Api/Nucleo.Api.csproj --startup-project backend/Nucleo.Api/Nucleo.Api.csproj -o Data/Migrations
dotnet ef database update --project backend/Nucleo.Api/Nucleo.Api.csproj --startup-project backend/Nucleo.Api/Nucleo.Api.csproj
```

```
# Tests (66 pruebas: Services con Moq + máquinas de estados) — la API NO debe estar corriendo (bloquea el .exe)
dotnet test backend/Nucleo.Api.Tests/Nucleo.Api.Tests.csproj

# Una sola clase / prueba
dotnet test backend/Nucleo.Api.Tests/Nucleo.Api.Tests.csproj --filter "FullyQualifiedName~ActivoServiceTests"

# Frontend (Karma/Jasmine; en CI: headless)
npm test --prefix frontend -- --watch=false --browsers=ChromeHeadless
```

```
# Datos de carga (ver backend/Nucleo.Api/Data/CLAUDE.md)
dotnet run --project backend/Nucleo.Api -- seed-bulk realista   # 50 clientes · 800 activos · 3k tickets
dotnet run --project backend/Nucleo.Api -- seed-bulk estres     # 1k clientes · 50k activos · 200k tickets
```

**Running locally requires** SQL Server Express at `.\SQLEXPRESS` and the JWT key in user-secrets (`dotnet user-secrets set "Jwt:Key" "<value>"` from `backend/Nucleo.Api`) — see `backend/Nucleo.Api/CLAUDE.md`. Seeded técnicos share the demo password `Nucleo123!`: `carlos.mendez@nucleo.mx` (Tecnico), `sofia.ramirez@nucleo.mx` (Lector), `diego.torres@nucleo.mx` (Admin).

## Integración continua (GitHub Actions)

Two workflows under `.github/workflows/`, both on push/PR to `main`:
- **`ci.yml`** — the general build+test gate, two parallel jobs. **backend**: `dotnet restore/build Nucleo.slnx` (Release) + `dotnet test` on the `Nucleo.Api.Tests` project (the 66 Moq-based Service tests — no SQL Server, no `Jwt:Key` secret needed, since those are only required to *start the web host*, not to build/test the assemblies). **frontend**: `npm ci` + `npm run build` + `npm test` headless (`--watch=false --browsers=ChromeHeadless`; ubuntu-latest ships Chrome). Both jobs validated locally with the exact CI commands before landing.
- **`lighthouse.yml`** — the frontend performance/a11y gate (see `frontend/CLAUDE.md`). Path-filtered to `frontend/**` so it only runs when the frontend changes.

`dotnet-version: "10.0.x"` in setup-dotnet because the project targets **net10.0** — GitHub runners don't ship it preinstalled, so the SDK is installed per-run. Restore/build/test use the **`.slnx`** solution directly (SDK 10 supports the new XML solution format natively).

## Project state

- **Fases 1–6 (done):** Cliente CRUD + base EF model → Activo CRUD + transactional state change/audit → JWT auth + roles → Tickets + dashboard aggregations → Angular shell (interceptor, guards, login) → feature UIs (Clientes, Activos, Tickets kanban, Dashboard, role-aware UI). Fase 5 was validated in a real browser, which is what surfaced the CORS and `MapInboundClaims` issues (see `backend/Nucleo.Api/CLAUDE.md`).
- **Post-Fase 6 (done):** server-side pagination (Clientes/Activos, then Tickets per kanban column and dashboard aggregated in SQL after load testing); per-ticket SLA badge; quality pass (axe AA-clean, responsive 375/1280) + CI (`ci.yml`, `lighthouse.yml`).
- Remaining stretch goals from the spec: SignalR, exports.
- See `C:\Users\domin\Downloads\proyecto-nucleo-spec (1).md` for the original spec — this project follows it, reconciled against choices made before it was shared (see project memory).
