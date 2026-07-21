import { CurrencyPipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Activo, ReporteDashboard, Ticket } from '../../core/models/nucleo.models';
import { AuthService } from '../../core/services/auth.service';
import { NucleoApiService } from '../../core/services/nucleo-api.service';

/**
 * Dashboard con computed signals: las distribuciones de activos y tickets se derivan
 * CLIENT-SIDE de las listas crudas (signals) — nadie recalcula contadores a mano.
 * Las métricas financieras (ingresos, promedio de horas, clientes sin tickets) vienen
 * del endpoint agregado del servidor, que cruza tablas que el front no carga.
 */
@Component({
  selector: 'app-dashboard',
  imports: [CurrencyPipe, RouterLink],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css'
})
export class DashboardComponent {
  private readonly api = inject(NucleoApiService);
  readonly auth = inject(AuthService);

  /**
   * activosPorEstado se deriva client-side del listado completo (misma razón por la que
   * Tickets se dejó sin paginar): una página parcial daría una distribución incorrecta.
   * Tamaño generoso en vez de paginar de verdad — ver conversación sobre alcance de paginación.
   */
  private static readonly TAMANO_TODOS_LOS_ACTIVOS = 500;

  readonly tickets = signal<Ticket[]>([]);
  readonly activos = signal<Activo[]>([]);
  readonly resumenServidor = signal<ReporteDashboard | null>(null);
  readonly error = signal<string | null>(null);

  // ------- Derivados con computed: se recalculan solos si cambian los signals base -------

  readonly ticketsAbiertos = computed(
    () => this.tickets().filter((t) => t.estado === 'Abierto' || t.estado === 'EnProgreso').length
  );

  readonly ticketsPorEstado = computed(() => this.agrupar(this.tickets(), (t) => t.estado));
  readonly ticketsPorPrioridad = computed(() =>
    this.agrupar(this.tickets().filter((t) => t.estado === 'Abierto' || t.estado === 'EnProgreso'), (t) => t.prioridad)
  );
  readonly activosPorEstado = computed(() => this.agrupar(this.activos(), (a) => a.estado));

  readonly totalActivos = computed(() => this.activos().length);

  constructor() {
    this.api.obtenerTickets().subscribe({
      next: (t) => this.tickets.set(t),
      error: () => this.error.set('No se pudieron cargar los datos del dashboard.')
    });
    this.api.obtenerActivos(null, 1, DashboardComponent.TAMANO_TODOS_LOS_ACTIVOS).subscribe((r) => this.activos.set(r.items));
    this.api.obtenerDashboard().subscribe((r) => this.resumenServidor.set(r));
  }

  private agrupar<T>(items: T[], clave: (item: T) => string): { nombre: string; cantidad: number }[] {
    const mapa = new Map<string, number>();
    for (const item of items) {
      const k = clave(item);
      mapa.set(k, (mapa.get(k) ?? 0) + 1);
    }
    return [...mapa.entries()].map(([nombre, cantidad]) => ({ nombre, cantidad }));
  }
}
