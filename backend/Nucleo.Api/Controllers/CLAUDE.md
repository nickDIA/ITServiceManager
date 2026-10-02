# Controllers

- Controllers only translate HTTP ⇄ service calls. **No EF Core, no business logic, no entities** — they receive/return DTOs and let domain exceptions bubble to `GlobalExceptionHandler`.
- `[Authorize]` at controller level on every domain controller (any authenticated técnico can read); `[Authorize(Roles = Roles.Escritura)]` (`Admin,Tecnico`) on POST/PUT/DELETE/PATCH actions — `Lector` is read-only. `AuthController` itself has no `[Authorize]` — login must stay anonymous.
- Identity comes from the token, not the body: `ActivosController.CambiarEstado` reads `tecnicoId` from the JWT `sub` claim (`User.FindFirstValue(JwtRegisteredClaimNames.Sub)`), not from `CambiarEstadoActivoDto`. This only works because of `MapInboundClaims = false` (see `../CLAUDE.md`).
- List endpoints (`GET /api/clientes`, `/api/activos`, `/api/tickets`) take optional `pagina`/`tamano` query params (default `1`/`20`); `/api/tickets` also takes `?estado=` (the kanban calls it once per column). Clamping happens in the Service, not here.
- `GET /api/tecnicos` (`TecnicosController`, read-only, added in Fase 6) exists solely to populate the ticket-assignment selector.
- `GET /api/reportes/dashboard` → `IReporteService`; see `../Services/CLAUDE.md`.
