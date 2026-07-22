import { CurrencyPipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ReporteDashboard } from '../../core/models/nucleo.models';
import { AuthService } from '../../core/services/auth.service';
import { NucleoApiService } from '../../core/services/nucleo-api.service';

/**
 * Dashboard con computed signals derivados de UNA sola llamada agregada
 * (`GET /api/reportes/dashboard`), que hace los GROUP BY/SUM/AVG en SQL.
 *
 * Antes las distribuciones se calculaban CLIENT-SIDE sobre las listas crudas: se traía
 * la lista completa de tickets y una página de 500 activos. Las pruebas de carga lo
 * tumbaron por dos motivos, no uno:
 *   1) Rendimiento: la lista de tickets pesaba ~85 MB con datos reales.
 *   2) Corrección: el tope de 500 activos hacía que "Activos gestionados" y su
 *      distribución mintieran en cuanto había más de 500.
 * Agregar en la base y traer solo los totales arregla ambos. El patrón `computed` sigue
 * intacto: lo que cambió es la FUENTE (un agregado) en vez de listas completas.
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

  readonly resumenServidor = signal<ReporteDashboard | null>(null);
  readonly error = signal<string | null>(null);

  // ------- Derivados con computed: se recalculan solos al llegar el resumen -------

  readonly ticketsAbiertos = computed(() => this.resumenServidor()?.ticketsAbiertos ?? 0);

  readonly ticketsPorEstado = computed(() => this.aFilas(this.resumenServidor()?.ticketsPorEstado));
  readonly ticketsPorPrioridad = computed(() => this.aFilas(this.resumenServidor()?.ticketsPorPrioridad));
  readonly activosPorEstado = computed(() => this.aFilas(this.resumenServidor()?.activosPorEstado));

  /** Total de activos = suma de la distribución por estado (ya no un length capado). */
  readonly totalActivos = computed(() =>
    this.activosPorEstado().reduce((suma, fila) => suma + fila.cantidad, 0)
  );

  constructor() {
    this.api.obtenerDashboard().subscribe({
      next: (r) => this.resumenServidor.set(r),
      error: () => this.error.set('No se pudieron cargar los datos del dashboard.')
    });
  }

  /** Convierte el diccionario { Estado: cantidad } del DTO en filas para el template. */
  private aFilas(dist: Partial<Record<string, number>> | undefined): { nombre: string; cantidad: number }[] {
    return Object.entries(dist ?? {}).map(([nombre, cantidad]) => ({ nombre, cantidad: cantidad ?? 0 }));
  }
}
