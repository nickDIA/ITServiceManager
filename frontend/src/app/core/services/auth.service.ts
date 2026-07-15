import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { LoginRequest, LoginResponse, UsuarioActual } from '../models/auth.models';
import { decodeJwtPayload } from '../utils/jwt.util';

const TOKEN_KEY = 'nucleo_token';

/** Forma del payload del JWT emitido por TokenService (claims cortos: sub/email/name/role). */
interface JwtPayload {
  sub: string;
  email: string;
  name: string;
  role: UsuarioActual['rol'];
  exp: number;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);

  private readonly _usuario = signal<UsuarioActual | null>(this.cargarUsuarioInicial());
  readonly usuario = this._usuario.asReadonly();
  readonly estaAutenticado = computed(() => this._usuario() !== null);

  login(credenciales: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${environment.apiUrl}/auth/login`, credenciales).pipe(
      tap((respuesta) => {
        localStorage.setItem(TOKEN_KEY, respuesta.token);
        this._usuario.set({
          tecnicoId: respuesta.tecnicoId,
          nombre: respuesta.nombre,
          email: respuesta.email,
          rol: respuesta.rol
        });
      })
    );
  }

  logout(): void {
    localStorage.removeItem(TOKEN_KEY);
    this._usuario.set(null);
  }

  obtenerToken(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  }

  /** Al recargar la página, reconstruye la sesión desde el JWT guardado, sin llamar a la API. */
  private cargarUsuarioInicial(): UsuarioActual | null {
    const token = localStorage.getItem(TOKEN_KEY);
    if (!token) return null;

    const payload = decodeJwtPayload<JwtPayload>(token);
    if (!payload) {
      localStorage.removeItem(TOKEN_KEY);
      return null;
    }

    const yaExpiro = Date.now() >= payload.exp * 1000;
    if (yaExpiro) {
      localStorage.removeItem(TOKEN_KEY);
      return null;
    }

    return {
      tecnicoId: Number(payload.sub),
      nombre: payload.name,
      email: payload.email,
      rol: payload.role
    };
  }
}
