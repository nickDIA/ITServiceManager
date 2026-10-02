# Services

Services hold **all** business rules: validation, conflict checks, cross-repository orchestration, transactions, and entity↔DTO mapping. They never touch `AppDbContext` directly (go through repositories / `IUnitOfWork`), and they persist explicitly with `GuardarCambiosAsync()` — repository writes only stage changes (see `../Repositories/CLAUDE.md`).

## Transactional state changes (the key exercise)

Changing an `Activo`'s `Estado` must write an audit row to `HistorialActivo` in the **same transaction** — if the audit write fails, the state change rolls back. Because the Service touches two repositories (`IActivoRepositorio` + `IHistorialActivoRepositorio`) sharing one `Scoped` `AppDbContext`, and each repo's `GuardarCambiosAsync()` is a separate `SaveChanges` call, the two writes are **not** atomic together by default — two sequential `SaveChangesAsync()` calls are two separate implicit transactions. This is solved with an explicit `IUnitOfWork` (`Repositories/IUnitOfWork.cs` / `UnitOfWork.cs`) that wraps EF Core's `IDbContextTransaction` without leaking EF types into the Service layer:

`IniciarTransaccionAsync()` → update `Activo` + `GuardarCambiosAsync()` → insert `HistorialActivo` + `GuardarCambiosAsync()` → `ConfirmarTransaccionAsync()`, with `RevertirTransaccionAsync()` on any failure in between.

See `ActivoService.CambiarEstadoAsync` for the full pattern — treat it as the reference implementation for any future multi-repository transactional operation.

Valid transitions are checked against `Domain/` (see `../Domain/CLAUDE.md`) **before** opening a transaction.

`TecnicoId` existence for a state change is intentionally **not** pre-validated in C# — it's enforced by the SQL Server FK constraint on `HistorialActivo.TecnicoId`. `ActivoService` catches the resulting `DbUpdateException` (SQL error 547), translates it to a 404 `RecursoNoEncontradoException`, and rolls back. Since Fase 3, `tecnicoId` is no longer client-supplied (it's read from the JWT's `sub` claim), so this FK-violation path is now a defensive backstop rather than the primary way to trigger it — reproducing the rollback deliberately requires deleting/tampering the técnico row directly (e.g. via `sqlcmd`) while a still-valid token references it.

## Tickets (Fase 4)

Unlike `Activo`, there's no audit table for tickets, so `TicketService.CambiarEstadoAsync` is a single `SaveChanges` — no `IUnitOfWork` needed. This asymmetry is intentional and worth pointing out when extending either flow: only add explicit transaction handling when an operation truly spans more than one repository/table.

`TicketService.CrearAsync`/`ActualizarAsync` enforce a cross-entity business rule beyond FK existence: if `ActivoId` is supplied, that activo must belong to the same `ClienteId` as the ticket (`ConflictoException` → 409 otherwise).

**SLA:** `TicketResponseDto.SlaHoras` (nullable) is the **strictest** `Contrato.SlaHoras` among the ticket's cliente's currently-`Activo` contratos (`TicketService.ObtenerSlaHoras` — there's no DB uniqueness constraint forcing exactly one active contract per client, so ties are broken conservatively, favoring the alert over a silent miss). Everything else about SLA alerts is frontend-only — see `frontend/src/app/features/tickets/CLAUDE.md`.

## Paginación

`ClienteService`/`ActivoService`/`TicketService` clamp `tamano` to `[1, 100]` (`Math.Clamp`) server-side regardless of what the client asks for, so a caller can't force a full-table scan by requesting an absurd page size.

`GET /api/tickets` was originally left unpaginated on purpose (the kanban grouped client-side and a partial page would have lied). Load testing killed that decision: at 200k tickets the response was **85 MB / ~9.6 s**. It now paginates and filters by `estado`, and the kanban makes one call per column — **85 MB → 8.3 KB and ~9600 ms → ~13 ms per column.** Correctness hinges on `TotalRegistros` being the total of the filter.

## Reportes (dashboard)

`GET /api/reportes/dashboard` (`ReportesController` → `IReporteService` → `IReporteRepositorio`) is the aggregation exercise: `GROUP BY` (activos por estado, tickets por estado/prioridad), `SUM` (ingresos mensuales recurrentes de contratos activos), `AVG` (horas incluidas promedio), and a subquery/anti-join (clientes activos sin ningún ticket Abierto/EnProgreso). It is the **only** data source for the frontend dashboard — nothing is aggregated client-side (that used to cap "Activos gestionados" at 500 and lie with 50k activos). Note `ticketsPorPrioridad` counts **all** tickets, not only open ones.

`ReporteService` awaits each repository call **sequentially, not via `Task.WhenAll`** — all calls share one `Scoped` `AppDbContext`, and EF Core does not support concurrent operations on the same context instance (this would throw at runtime, not just be inefficient).

Tests for this layer live in `backend/Nucleo.Api.Tests/Services/` — see `../../Nucleo.Api.Tests/CLAUDE.md`.
