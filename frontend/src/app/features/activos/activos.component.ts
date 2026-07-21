import { DatePipe } from '@angular/common';
import { Component, effect, inject, signal } from '@angular/core';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import {
  Activo, Cliente, EstadoActivo, HistorialActivo, TIPOS_ACTIVO, TRANSICIONES_ACTIVO
} from '../../core/models/nucleo.models';
import { AuthService } from '../../core/services/auth.service';
import { NucleoApiService } from '../../core/services/nucleo-api.service';
import { ConfirmDialogService } from '../../shared/confirm-dialog/confirm-dialog.service';

/**
 * Gestión de activos. El filtro por cliente es un signal, y un EFFECT reacciona a sus
 * cambios recargando la lista: el <select> solo escribe el signal, sin llamar a la API
 * directamente — la sincronización filtro→datos vive en un único lugar.
 */
@Component({
  selector: 'app-activos',
  imports: [DatePipe, FormsModule, ReactiveFormsModule],
  templateUrl: './activos.component.html'
})
export class ActivosComponent {
  private readonly api = inject(NucleoApiService);
  private readonly fb = inject(FormBuilder);
  private readonly confirmDialog = inject(ConfirmDialogService);
  readonly auth = inject(AuthService);

  readonly transiciones = TRANSICIONES_ACTIVO;
  readonly tiposActivo = TIPOS_ACTIVO;

  private static readonly TAMANO_PAGINA = 20;
  /** Tamaño generoso para dropdowns que necesitan "todos los clientes", no una página. */
  private static readonly TAMANO_DROPDOWN = 200;

  readonly clientes = signal<Cliente[]>([]);
  readonly activos = signal<Activo[]>([]);
  readonly cargando = signal(true);
  readonly error = signal<string | null>(null);

  private paginaActual = 1;
  readonly hayMas = signal(true);
  readonly cargandoMas = signal(false);

  /** Filtro por cliente: null = todos. El effect de abajo reacciona a cada cambio. */
  readonly filtroClienteId = signal<number | null>(null);

  // Panel de cambio de estado (uno a la vez)
  readonly cambiandoEstadoDe = signal<Activo | null>(null);
  readonly nuevoEstado = signal<EstadoActivo | null>(null);
  motivo = '';

  // Historial expandido (uno a la vez)
  readonly historialDe = signal<number | null>(null);
  readonly historial = signal<HistorialActivo[]>([]);

  readonly mostrandoForm = signal(false);
  readonly form = this.fb.nonNullable.group({
    clienteId: [0, [Validators.required, Validators.min(1)]],
    tipo: ['Hardware' as (typeof TIPOS_ACTIVO)[number], Validators.required],
    nombre: ['', [Validators.required, Validators.minLength(2)]],
    numeroSerie: ['', [Validators.required, Validators.minLength(3)]],
    fechaAdquisicion: ['', Validators.required]
  });

  constructor() {
    this.api.obtenerClientes(1, ActivosComponent.TAMANO_DROPDOWN).subscribe((r) => this.clientes.set(r.items));

    // El effect ve el signal del filtro: cambiarlo (desde el select) dispara la recarga
    // desde la página 1 — cambiar de filtro siempre reinicia el scroll infinito.
    effect(() => {
      const clienteId = this.filtroClienteId();
      this.cargando.set(true);
      this.paginaActual = 1;
      this.api.obtenerActivos(clienteId, 1, ActivosComponent.TAMANO_PAGINA).subscribe({
        next: (r) => {
          this.activos.set(r.items);
          this.hayMas.set(r.hayMas);
          this.cargando.set(false);
        },
        error: () => {
          this.error.set('No se pudieron cargar los activos.');
          this.cargando.set(false);
        }
      });
    });
  }

  cargarMas(): void {
    if (!this.hayMas() || this.cargandoMas()) return;
    this.cargandoMas.set(true);
    const siguiente = this.paginaActual + 1;
    this.api.obtenerActivos(this.filtroClienteId(), siguiente, ActivosComponent.TAMANO_PAGINA).subscribe({
      next: (r) => {
        this.paginaActual = siguiente;
        this.activos.update((lista) => [...lista, ...r.items]);
        this.hayMas.set(r.hayMas);
        this.cargandoMas.set(false);
      },
      error: () => {
        this.error.set('No se pudieron cargar más activos.');
        this.cargandoMas.set(false);
      }
    });
  }

  private recargar(): void {
    // Reasignar el mismo valor no dispara el effect (igualdad referencial), así que
    // recargamos explícito tras una mutación — siempre volviendo a la página 1.
    this.paginaActual = 1;
    this.api.obtenerActivos(this.filtroClienteId(), 1, ActivosComponent.TAMANO_PAGINA).subscribe((r) => {
      this.activos.set(r.items);
      this.hayMas.set(r.hayMas);
    });
  }

  cambiarFiltro(valor: string): void {
    this.filtroClienteId.set(valor === '' ? null : Number(valor));
  }

  // ------------------------------------------------ Alta

  crear(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }
    this.error.set(null);
    const v = this.form.getRawValue();
    this.api.crearActivo({ ...v, clienteId: Number(v.clienteId) }).subscribe({
      next: () => {
        this.form.reset({ clienteId: 0, tipo: 'Hardware' });
        this.mostrandoForm.set(false);
        this.recargar();
      },
      error: (err) => this.error.set(err.error?.detail ?? 'No se pudo crear el activo.')
    });
  }

  async eliminar(activo: Activo): Promise<void> {
    const confirmado = await this.confirmDialog.confirmar(
      `¿Eliminar el activo "${activo.nombre}"? Se borrará también su historial.`
    );
    if (!confirmado) return;
    this.error.set(null);
    this.api.eliminarActivo(activo.id).subscribe({
      next: () => this.recargar(),
      error: (err) => this.error.set(err.error?.detail ?? 'No se pudo eliminar el activo.')
    });
  }

  // ------------------------------------------------ Cambio de estado (transaccional en el backend)

  abrirCambioEstado(activo: Activo): void {
    this.cambiandoEstadoDe.set(activo);
    this.nuevoEstado.set(null);
    this.motivo = '';
    this.historialDe.set(null);
  }

  confirmarCambioEstado(): void {
    const activo = this.cambiandoEstadoDe();
    const estado = this.nuevoEstado();
    if (!activo || !estado || this.motivo.trim().length < 5) return;

    this.error.set(null);
    this.api.cambiarEstadoActivo(activo.id, estado, this.motivo.trim()).subscribe({
      next: () => {
        this.cambiandoEstadoDe.set(null);
        this.recargar();
      },
      error: (err) => this.error.set(err.error?.detail ?? 'No se pudo cambiar el estado.')
    });
  }

  // ------------------------------------------------ Historial

  verHistorial(activo: Activo): void {
    if (this.historialDe() === activo.id) {
      this.historialDe.set(null);
      return;
    }
    this.cambiandoEstadoDe.set(null);
    this.api.obtenerHistorialActivo(activo.id).subscribe((h) => {
      this.historial.set(h);
      this.historialDe.set(activo.id);
    });
  }
}
