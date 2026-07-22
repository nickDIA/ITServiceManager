import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import {
  Activo, Cliente, ESTADOS_TICKET, EstadoTicket, PRIORIDADES, Tecnico, Ticket, TRANSICIONES_TICKET
} from '../../core/models/nucleo.models';
import { AuthService } from '../../core/services/auth.service';
import { NucleoApiService } from '../../core/services/nucleo-api.service';
import { ConfirmDialogService } from '../../shared/confirm-dialog/confirm-dialog.service';

/** Solo se alerta sobre tickets todavía en curso; un ticket cerrado ya no está "en riesgo". */
const ESTADOS_CON_RIESGO_SLA: EstadoTicket[] = ['Abierto', 'EnProgreso'];
/** A partir de este % del SLA transcurrido, se marca "en riesgo" (antes de incumplirlo). */
const UMBRAL_RIESGO_SLA = 0.8;

export interface RiesgoSla {
  nivel: 'riesgo' | 'incumplido';
  horasTranscurridas: number;
}

/** Estado de UNA columna del kanban: su propia página, su propio total y su propio "cargar más". */
export interface ColumnaTickets {
  estado: EstadoTicket;
  tickets: Ticket[];
  /** Total de tickets en ese estado (del servidor), no solo los cargados. */
  total: number;
  pagina: number;
  hayMas: boolean;
  cargando: boolean;
}

/**
 * Tablero de tickets agrupado por estado. Las columnas y sus contadores son COMPUTED
 * derivados del signal `tickets`: al cambiar el estado de un ticket solo se actualiza
 * ese signal, y columnas/contadores se recalculan solos — la validación central de la
 * Fase 6 ("los contadores se actualizan solos al cambiar datos").
 */
@Component({
  selector: 'app-tickets',
  imports: [DatePipe, DecimalPipe, ReactiveFormsModule],
  templateUrl: './tickets.component.html',
  styleUrl: './tickets.component.css'
})
export class TicketsComponent {
  private readonly api = inject(NucleoApiService);
  private readonly fb = inject(FormBuilder);
  private readonly confirmDialog = inject(ConfirmDialogService);
  readonly auth = inject(AuthService);

  /** Tamaño generoso: estos dropdowns necesitan "todos", no una página. */
  private static readonly TAMANO_DROPDOWN = 200;

  readonly transiciones = TRANSICIONES_TICKET;
  readonly estadosTicket = ESTADOS_TICKET;
  readonly prioridades = PRIORIDADES;

  /** Cuántos tickets carga cada columna por página. */
  private static readonly TAMANO_COLUMNA = 20;

  readonly clientes = signal<Cliente[]>([]);
  readonly tecnicos = signal<Tecnico[]>([]);
  readonly activosDelCliente = signal<Activo[]>([]);
  readonly error = signal<string | null>(null);
  readonly mostrandoForm = signal(false);

  /**
   * Una columna por estado, cada una con SU página. Antes esto era un `computed` que
   * agrupaba un único signal con TODOS los tickets; a escala real eso significaba traer
   * 200k filas (85 MB) en una sola respuesta. Ahora cada columna pide solo su página y
   * el servidor devuelve además el total del estado, que es el contador de la columna.
   */
  readonly columnas = signal<ColumnaTickets[]>(
    ESTADOS_TICKET.map((estado) => ({
      estado, tickets: [], total: 0, pagina: 0, hayMas: false, cargando: true
    }))
  );

  /** Contador global de abiertos: suma de los TOTALES del servidor, no de lo cargado. */
  readonly totalAbiertos = computed(() =>
    this.columnas()
      .filter((c) => c.estado === 'Abierto' || c.estado === 'EnProgreso')
      .reduce((suma, c) => suma + c.total, 0)
  );

  /**
   * Riesgo de SLA por ticket, id -> RiesgoSla. COMPUTED como el resto del tablero: "ahora" se
   * lee una sola vez por recálculo, no en cada evaluación de template — llamar Date.now()
   * directo en el template producía un NG0100 (el valor "cambiaba" entre pasadas de chequeo
   * de Angular en dev porque cada llamada devolvía un milisegundo distinto).
   */
  readonly riesgosSla = computed(() => {
    const ahora = Date.now();
    const mapa = new Map<number, RiesgoSla>();
    for (const columna of this.columnas()) {
      for (const ticket of columna.tickets) {
        const riesgo = this.calcularRiesgoSla(ticket, ahora);
        if (riesgo) mapa.set(ticket.id, riesgo);
      }
    }
    return mapa;
  });

  private calcularRiesgoSla(ticket: Ticket, ahora: number): RiesgoSla | null {
    if (ticket.slaHoras == null || !ESTADOS_CON_RIESGO_SLA.includes(ticket.estado)) return null;

    const horasTranscurridas = (ahora - new Date(ticket.fechaCreacion).getTime()) / 3_600_000;
    const proporcion = horasTranscurridas / ticket.slaHoras;

    if (proporcion >= 1) return { nivel: 'incumplido', horasTranscurridas };
    if (proporcion >= UMBRAL_RIESGO_SLA) return { nivel: 'riesgo', horasTranscurridas };
    return null;
  }

  readonly form = this.fb.nonNullable.group({
    clienteId: [0, [Validators.required, Validators.min(1)]],
    activoId: [null as number | null],
    titulo: ['', [Validators.required, Validators.minLength(3)]],
    descripcion: ['', [Validators.required, Validators.minLength(5)]],
    prioridad: ['Media' as (typeof PRIORIDADES)[number], Validators.required],
    tecnicoId: [0, [Validators.required, Validators.min(1)]]
  });

  constructor() {
    // Una petición por columna (5), cada una de 20 filas, en vez de una sola de 200k.
    for (const estado of ESTADOS_TICKET) this.cargarColumna(estado, 1, true);

    this.api.obtenerClientes(1, TicketsComponent.TAMANO_DROPDOWN).subscribe((r) => this.clientes.set(r.items));
    this.api.obtenerTecnicos().subscribe((t) => this.tecnicos.set(t));

    // El selector de activos depende del cliente elegido en el form (regla del backend:
    // el activo debe pertenecer al mismo cliente del ticket). Un solo cliente nunca tiene
    // cientos de activos, así que una página grande cubre el caso real sin paginar el select.
    this.form.controls.clienteId.valueChanges.subscribe((clienteId) => {
      this.form.controls.activoId.setValue(null);
      const id = Number(clienteId);
      if (id > 0) {
        this.api.obtenerActivos(id, 1, TicketsComponent.TAMANO_DROPDOWN).subscribe((r) => this.activosDelCliente.set(r.items));
      } else {
        this.activosDelCliente.set([]);
      }
    });
  }

  /** Aplica un cambio parcial a UNA columna, dejando las demás intactas. */
  private parchearColumna(estado: EstadoTicket, cambio: Partial<ColumnaTickets>): void {
    this.columnas.update((cols) => cols.map((c) => (c.estado === estado ? { ...c, ...cambio } : c)));
  }

  /**
   * Carga una página de una columna. `reset` reemplaza sus tickets (carga inicial o refresco
   * tras una mutación); si es false, los agrega ("cargar más" dentro de la columna).
   */
  private cargarColumna(estado: EstadoTicket, pagina: number, reset: boolean): void {
    this.parchearColumna(estado, { cargando: true });

    this.api.obtenerTickets(estado, pagina, TicketsComponent.TAMANO_COLUMNA).subscribe({
      next: (r) => {
        this.columnas.update((cols) =>
          cols.map((c) =>
            c.estado !== estado
              ? c
              : {
                  ...c,
                  tickets: reset ? r.items : [...c.tickets, ...r.items],
                  total: r.totalRegistros,
                  pagina: r.pagina,
                  hayMas: r.hayMas,
                  cargando: false
                }
          )
        );
      },
      error: () => {
        this.parchearColumna(estado, { cargando: false });
        this.error.set('No se pudieron cargar los tickets.');
      }
    });
  }

  cargarMas(columna: ColumnaTickets): void {
    if (!columna.hayMas || columna.cargando) return;
    this.cargarColumna(columna.estado, columna.pagina + 1, false);
  }

  crear(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.error.set(null);
    const v = this.form.getRawValue();
    this.api.crearTicket({
      clienteId: Number(v.clienteId),
      activoId: v.activoId === null ? null : Number(v.activoId),
      titulo: v.titulo,
      descripcion: v.descripcion,
      prioridad: v.prioridad,
      tecnicoId: Number(v.tecnicoId)
    }).subscribe({
      next: () => {
        // Un ticket nace Abierto: refresca esa columna para que su total sea el del servidor.
        this.cargarColumna('Abierto', 1, true);
        this.form.reset({ clienteId: 0, activoId: null, prioridad: 'Media', tecnicoId: 0 });
        this.mostrandoForm.set(false);
      },
      error: (err) => this.error.set(err.error?.detail ?? 'No se pudo crear el ticket.')
    });
  }

  cambiarEstado(ticket: Ticket, nuevoEstado: EstadoTicket): void {
    this.error.set(null);
    const estadoOrigen = ticket.estado;
    this.api.cambiarEstadoTicket(ticket.id, nuevoEstado).subscribe({
      // El ticket cruza de columna: refresca ORIGEN y DESTINO para que ambos totales
      // sigan viniendo del servidor (moverlo a mano dejaría los contadores mintiendo).
      next: () => {
        this.cargarColumna(estadoOrigen, 1, true);
        this.cargarColumna(nuevoEstado, 1, true);
      },
      error: (err) => this.error.set(err.error?.detail ?? 'No se pudo cambiar el estado.')
    });
  }

  async eliminar(ticket: Ticket): Promise<void> {
    const confirmado = await this.confirmDialog.confirmar(`¿Eliminar el ticket "${ticket.titulo}"?`);
    if (!confirmado) return;
    this.error.set(null);
    this.api.eliminarTicket(ticket.id).subscribe({
      next: () => this.cargarColumna(ticket.estado, 1, true),
      error: (err) => this.error.set(err.error?.detail ?? 'No se pudo eliminar el ticket.')
    });
  }
}
