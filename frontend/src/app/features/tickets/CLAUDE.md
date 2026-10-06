# tickets/ (kanban)

## Paginado POR COLUMNA

The board makes **one API call per column** (`GET /api/tickets?estado=Abierto&pagina=1&tamano=20`), never one call for everything (that was 85 MB / ~9.6 s at 200k tickets). `TicketsComponent.columnas` is a `signal<ColumnaTickets[]>` where each column holds its own page/total/`hayMas`; `totalAbiertos` and `riesgosSla` are `computed` over it. Each column's counter comes from `ResultadoPaginado.totalRegistros` (the server total **of the filter**, e.g. 40 033) while only ~20 cards are in memory — no separate counts endpoint needed.

Mutations refresh **only the affected columns** (a state change refreshes origin and destination) so totals keep coming from the server. Don't "optimistically" move a card between columns and recompute counters locally — the local count would be the count of loaded cards, not the real total.

## Alertas de SLA (badge por ticket)

The only server input is `TicketResponseDto.SlaHoras` (strictest active contract's SLA; see `backend/Nucleo.Api/Services/CLAUDE.md`). Everything else is computed here — no new endpoint, no background job:
- `riesgosSla` is a `computed()` — `Map<ticketId, RiesgoSla>` — derived from the loaded cards in `columnas()` and `Date.now()` read *once* per recomputation. It replaced an earlier version that called `Date.now()` straight from the template on a plain method: that produced a real `NG0100 ExpressionChangedAfterItHasBeenCheckedError` in dev (two template evaluations of the same expression landed a few ms apart, so the "hours elapsed" value differed between Angular's check and verify passes). **Any per-render, non-signal-derived value in a template is at risk of the same bug — compute it once into a signal/computed instead.**
- Only `Abierto`/`EnProgreso` tickets are eligible (a closed ticket isn't "at risk" anymore); `>= 100%` of `SlaHoras` elapsed → `incumplido` (red badge), `>= 80%` → `riesgo` (amber badge), otherwise no badge at all — showing a badge only when there's something to act on, not a permanent "en tiempo" state on every card.
- Deliberately **not** on the Dashboard: it's a technician-facing, per-ticket signal, not a manager-facing aggregate.

`.columna-vacia` color (`#555c72`) was an a11y fix (contrast) — keep it AA-compliant if restyling.
