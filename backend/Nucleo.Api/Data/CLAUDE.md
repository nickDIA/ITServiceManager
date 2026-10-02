# Data (EF Core model, seeders, migrations)

## EF Core model conventions

- Entity configuration lives in `Configurations/`, one `IEntityTypeConfiguration<T>` per entity, loaded via `modelBuilder.ApplyConfigurationsFromAssembly` in `AppDbContext.OnModelCreating`.
- Enums are stored as `nvarchar(20)` (`HasConversion<string>()`) for DB readability, **and** serialized as strings over the API (`JsonStringEnumConverter` in `Program.cs`) — both DB rows and JSON payloads use names like `"EnReparacion"`, never raw ints.
- Delete behaviors are deliberately mixed to avoid SQL Server's "multiple cascade paths" restriction: `Activo → HistorialActivo` is the only `Cascade`; `Tecnico → HistorialActivo` and `Cliente → (Activo/Ticket/Contrato)` are `Restrict`; the nullable `Ticket → Activo` FK is `SetNull`. When adding new FK relationships, check for cascade-path conflicts before defaulting to `Cascade`.
- The FK on `HistorialActivo.TecnicoId` is load-bearing: `ActivoService` relies on it (SQL error 547) instead of pre-validating in C#.
- Migrations go in `Migrations/` (`-o Data/Migrations`; full command in the root CLAUDE.md). Dev startup applies them automatically.

## Seeders

- `DbSeeder.cs` (demo data, runs on dev startup) seeds each table **independently and idempotently** (each gated on its own `AnyAsync` check, not a single top-level check) — follow this pattern when adding new seed data so it isn't silently skipped just because some other table already has rows. Demo password: `DbSeeder.PasswordDemoTecnicos`.
- `CargaSeeder.cs` generates VOLUME for real measurement — separate from `DbSeeder`. It's a gated maintenance command (`dotnet run --project backend/Nucleo.Api -- seed-bulk realista|estres`), never runs on host startup. Es **idempotente por "top-up"**: lleva cada tabla al objetivo del nivel, así que re-ejecutar no duplica y subir de nivel solo agrega la diferencia. Los datos de carga llevan prefijos propios (`LC…` en RFC, `SN-LC-…` en serie, `carga…@carga.test`) para no chocar con el demo y poder identificarlos. Técnica de EF a escala que aplica: `AutoDetectChangesEnabled = false` + insertar en lotes con `ChangeTracker.Clear()` entre lotes — sin eso el change tracker crece y las inserciones se degradan a O(n²) (200k tickets en 65s con esto).

## Pruebas de rendimiento: qué encontró la medición a 200k tickets / 50k activos

| Hallazgo | Causa | Fix | Resultado |
|---|---|---|---|
| `/tickets` 4.6–9.6 s, **85 MB** | traía TODO el conjunto | paginar por columna (`../Services/CLAUDE.md`) | **~13 ms · 8.3 KB** |
| dashboard 1592 ms | `GROUP BY Estado/Prioridad` = scans de tabla completa | índices | **295 ms** |
| `/activos` paginado 147 ms | `ORDER BY Nombre` sin índice ordenaba las 50k filas en cada página | índice en `Nombre` | **13 ms** |

**La lección central del ejercicio:** los índices arreglan el costo del *plan de consulta*; **no** arreglan una decisión de traer todo el conjunto. Por eso los índices no movieron ni un poco a `/tickets` (su costo era materializar+serializar 200k filas) y solo la paginación lo resolvió. Son dos problemas distintos con dos fixes distintos.

Índices añadidos (migraciones `IndicesRendimiento` + `IndiceTicketEstadoFecha`): `Ticket(Estado, FechaCreacion)` compuesto — sirve el kanban paginado (`WHERE Estado ORDER BY FechaCreacion DESC` → seek + top-N) **y** el `GROUP BY Estado` (columna líder); `Ticket.Prioridad`; `Activo.Estado`; `Activo.Nombre`; `Cliente.Nombre`. Las FK ya las indexa EF sola — lo que faltaba eran las columnas de filtro/orden.
