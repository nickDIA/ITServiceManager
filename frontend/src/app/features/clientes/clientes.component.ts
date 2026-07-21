import { AsyncPipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { BehaviorSubject, Observable } from 'rxjs';
import { Cliente } from '../../core/models/nucleo.models';
import { AuthService } from '../../core/services/auth.service';
import { NucleoApiService } from '../../core/services/nucleo-api.service';
import { ConfirmDialogService } from '../../shared/confirm-dialog/confirm-dialog.service';

/**
 * Lista + alta/edición de clientes. La LISTA se consume con async pipe (el template
 * se suscribe/desuscribe solo) sobre un BehaviorSubject que acumula páginas; las ACCIONES
 * (crear, actualizar, borrar) usan subscribe imperativo porque son eventos puntuales, no
 * flujos que el template deba observar. El scroll infinito ("Cargar más") empuja páginas
 * nuevas al mismo subject en vez de reemplazar el patrón por uno imperativo.
 */
@Component({
  selector: 'app-clientes',
  imports: [AsyncPipe, ReactiveFormsModule],
  templateUrl: './clientes.component.html'
})
export class ClientesComponent {
  private readonly api = inject(NucleoApiService);
  private readonly fb = inject(FormBuilder);
  private readonly confirmDialog = inject(ConfirmDialogService);
  readonly auth = inject(AuthService);

  private static readonly TAMANO_PAGINA = 20;

  private paginaActual = 0;
  private acumulado: Cliente[] = [];
  private readonly clientesSubject = new BehaviorSubject<Cliente[]>([]);
  readonly clientes$ = this.clientesSubject.asObservable();

  readonly hayMas = signal(true);
  readonly cargandoMas = signal(false);

  readonly editandoId = signal<number | null>(null);
  readonly error = signal<string | null>(null);
  readonly guardando = signal(false);

  readonly form = this.fb.nonNullable.group({
    nombre: ['', [Validators.required, Validators.minLength(3)]],
    rfc: ['', [Validators.required, Validators.pattern(/^[A-ZÑ&]{3,4}\d{6}[A-Z0-9]{3}$/i)]],
    contacto: [''],
    telefono: [''],
    activo: [true]
  });

  constructor() {
    this.cargarPagina(1, true);
  }

  private cargarPagina(pagina: number, reiniciar: boolean): void {
    this.api.obtenerClientes(pagina, ClientesComponent.TAMANO_PAGINA).subscribe({
      next: (resultado) => {
        this.paginaActual = pagina;
        this.acumulado = reiniciar ? resultado.items : [...this.acumulado, ...resultado.items];
        this.clientesSubject.next(this.acumulado);
        this.hayMas.set(resultado.hayMas);
        this.cargandoMas.set(false);
      },
      error: () => {
        this.cargandoMas.set(false);
        this.error.set('No se pudieron cargar los clientes.');
      }
    });
  }

  cargarMas(): void {
    if (!this.hayMas() || this.cargandoMas()) return;
    this.cargandoMas.set(true);
    this.cargarPagina(this.paginaActual + 1, false);
  }

  private recargar(): void {
    this.cargarPagina(1, true);
  }

  iniciarEdicion(cliente: Cliente): void {
    this.editandoId.set(cliente.id);
    this.form.patchValue({
      nombre: cliente.nombre,
      rfc: cliente.rfc,
      contacto: cliente.contacto ?? '',
      telefono: cliente.telefono ?? '',
      activo: cliente.activo
    });
  }

  cancelarEdicion(): void {
    this.editandoId.set(null);
    this.form.reset({ activo: true });
  }

  guardar(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const v = this.form.getRawValue();
    const dto = {
      nombre: v.nombre,
      rfc: v.rfc.toUpperCase(),
      contacto: v.contacto || null,
      telefono: v.telefono || null
    };

    this.guardando.set(true);
    this.error.set(null);

    const id = this.editandoId();
    // Observable<unknown>: crear devuelve Observable<Cliente> y actualizar Observable<void>;
    // sin el tipo común, la unión de firmas de subscribe no es invocable (TS2349).
    const peticion: Observable<unknown> = id === null
      ? this.api.crearCliente(dto)
      : this.api.actualizarCliente(id, { ...dto, activo: v.activo });

    peticion.subscribe({
      next: () => {
        this.guardando.set(false);
        this.cancelarEdicion();
        this.recargar();
      },
      error: (err) => {
        this.guardando.set(false);
        this.error.set(err.error?.detail ?? 'No se pudo guardar el cliente.');
      }
    });
  }

  async eliminar(cliente: Cliente): Promise<void> {
    const confirmado = await this.confirmDialog.confirmar(`¿Eliminar al cliente "${cliente.nombre}"?`);
    if (!confirmado) return;

    this.error.set(null);
    this.api.eliminarCliente(cliente.id).subscribe({
      next: () => this.recargar(),
      error: (err) => this.error.set(err.error?.detail ?? 'No se pudo eliminar el cliente.')
    });
  }
}
