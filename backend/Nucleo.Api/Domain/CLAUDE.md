# Domain (state machines)

Valid state transitions are defined once per entity as static lookup tables, consulted by the Services **before** any write/transaction:

- `EstadoActivoTransiciones.cs` — `Retirado` is a terminal state with no outgoing transitions.
- `EstadoTicketTransiciones.cs` — `Abierto → EnProgreso → Resuelto → Cerrado`, plus an escape hatch `Abierto → Cancelado`; both `Cerrado` and `Cancelado` are terminal and auto-set `FechaCierre` (in `TicketService`).

**⚠ Mirrored in the frontend:** `frontend/src/app/core/models/nucleo.models.ts` (`TRANSICIONES_ACTIVO` / `TRANSICIONES_TICKET`) duplicates these tables so the UI only offers valid options. Change both together. The backend stays the real validator (409 on an invalid transition).

Unit tests: `backend/Nucleo.Api.Tests/Domain/`.
