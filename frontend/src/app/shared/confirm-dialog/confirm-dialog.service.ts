import { Injectable, signal } from '@angular/core';

interface EstadoConfirmacion {
  mensaje: string;
  resolver: (confirmado: boolean) => void;
}

/**
 * Reemplaza window.confirm() con un modal propio. Uso: `await confirmDialog.confirmar('¿Seguro?')`
 * — misma forma de uso que el confirm() nativo (bloquea hasta que el usuario responde),
 * pero renderizado con el look de la app. Un único diálogo global montado en AppComponent.
 */
@Injectable({ providedIn: 'root' })
export class ConfirmDialogService {
  private readonly _estado = signal<EstadoConfirmacion | null>(null);
  readonly estado = this._estado.asReadonly();

  confirmar(mensaje: string): Promise<boolean> {
    return new Promise((resolve) => {
      this._estado.set({ mensaje, resolver: resolve });
    });
  }

  responder(confirmado: boolean): void {
    this._estado()?.resolver(confirmado);
    this._estado.set(null);
  }
}
