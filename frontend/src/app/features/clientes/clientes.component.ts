import { AsyncPipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Observable } from 'rxjs';
import { Cliente } from '../../core/models/nucleo.models';
import { AuthService } from '../../core/services/auth.service';
import { NucleoApiService } from '../../core/services/nucleo-api.service';

/**
 * Lista + alta/edición de clientes. La LISTA se consume con async pipe (el template
 * se suscribe/desuscribe solo); las ACCIONES (crear, actualizar, borrar) usan subscribe
 * imperativo porque son eventos puntuales, no flujos que el template deba observar.
 */
@Component({
  selector: 'app-clientes',
  imports: [AsyncPipe, ReactiveFormsModule],
  templateUrl: './clientes.component.html'
})
export class ClientesComponent {
  private readonly api = inject(NucleoApiService);
  private readonly fb = inject(FormBuilder);
  readonly auth = inject(AuthService);

  clientes$: Observable<Cliente[]> = this.api.obtenerClientes();

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

  private recargar(): void {
    this.clientes$ = this.api.obtenerClientes();
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

  eliminar(cliente: Cliente): void {
    if (!confirm(`¿Eliminar al cliente "${cliente.nombre}"?`)) return;

    this.error.set(null);
    this.api.eliminarCliente(cliente.id).subscribe({
      next: () => this.recargar(),
      error: (err) => this.error.set(err.error?.detail ?? 'No se pudo eliminar el cliente.')
    });
  }
}
