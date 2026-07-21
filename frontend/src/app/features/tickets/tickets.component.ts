import { DatePipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import {
  Activo, Cliente, ESTADOS_TICKET, EstadoTicket, PRIORIDADES, Tecnico, Ticket, TRANSICIONES_TICKET
} from '../../core/models/nucleo.models';
import { AuthService } from '../../core/services/auth.service';
import { NucleoApiService } from '../../core/services/nucleo-api.service';
import { ConfirmDialogService } from '../../shared/confirm-dialog/confirm-dialog.service';

/**
 * Tablero de tickets agrupado por estado. Las columnas y sus contadores son COMPUTED
 * derivados del signal `tickets`: al cambiar el estado de un ticket solo se actualiza
 * ese signal, y columnas/contadores se recalculan solos — la validación central de la
 * Fase 6 ("los contadores se actualizan solos al cambiar datos").
 */
@Component({
  selector: 'app-tickets',
  imports: [DatePipe, ReactiveFormsModule],
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

  readonly tickets = signal<Ticket[]>([]);
  readonly clientes = signal<Cliente[]>([]);
  readonly tecnicos = signal<Tecnico[]>([]);
  readonly activosDelCliente = signal<Activo[]>([]);
  readonly error = signal<string | null>(null);
  readonly mostrandoForm = signal(false);

  /** Columnas del tablero, derivadas: NO se recalculan a mano en ningún lado. */
  readonly columnas = computed(() =>
    this.estadosTicket.map((estado) => ({
      estado,
      tickets: this.tickets().filter((t) => t.estado === estado)
    }))
  );

  /** Contador global de abiertos (Abierto + EnProgreso), también derivado. */
  readonly totalAbiertos = computed(
    () => this.tickets().filter((t) => t.estado === 'Abierto' || t.estado === 'EnProgreso').length
  );

  readonly form = this.fb.nonNullable.group({
    clienteId: [0, [Validators.required, Validators.min(1)]],
    activoId: [null as number | null],
    titulo: ['', [Validators.required, Validators.minLength(3)]],
    descripcion: ['', [Validators.required, Validators.minLength(5)]],
    prioridad: ['Media' as (typeof PRIORIDADES)[number], Validators.required],
    tecnicoId: [0, [Validators.required, Validators.min(1)]]
  });

  constructor() {
    this.recargar();
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

  private recargar(): void {
    this.api.obtenerTickets().subscribe({
      next: (t) => this.tickets.set(t),
      error: () => this.error.set('No se pudieron cargar los tickets.')
    });
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
      next: (creado) => {
        // Actualiza el signal localmente: columnas y contadores se recalculan solos.
        this.tickets.update((lista) => [creado, ...lista]);
        this.form.reset({ clienteId: 0, activoId: null, prioridad: 'Media', tecnicoId: 0 });
        this.mostrandoForm.set(false);
      },
      error: (err) => this.error.set(err.error?.detail ?? 'No se pudo crear el ticket.')
    });
  }

  cambiarEstado(ticket: Ticket, nuevoEstado: EstadoTicket): void {
    this.error.set(null);
    this.api.cambiarEstadoTicket(ticket.id, nuevoEstado).subscribe({
      next: (actualizado) => {
        // Sustituye SOLO ese ticket en el signal: el tablero completo reacciona.
        this.tickets.update((lista) => lista.map((t) => (t.id === actualizado.id ? actualizado : t)));
      },
      error: (err) => this.error.set(err.error?.detail ?? 'No se pudo cambiar el estado.')
    });
  }

  async eliminar(ticket: Ticket): Promise<void> {
    const confirmado = await this.confirmDialog.confirmar(`¿Eliminar el ticket "${ticket.titulo}"?`);
    if (!confirmado) return;
    this.error.set(null);
    this.api.eliminarTicket(ticket.id).subscribe({
      next: () => this.tickets.update((lista) => lista.filter((t) => t.id !== ticket.id)),
      error: (err) => this.error.set(err.error?.detail ?? 'No se pudo eliminar el ticket.')
    });
  }
}
