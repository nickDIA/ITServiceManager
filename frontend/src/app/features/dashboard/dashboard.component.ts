import { HttpClient } from '@angular/common/http';
import { Component, OnInit, inject, signal } from '@angular/core';
import { environment } from '../../../environments/environment';
import { AuthService } from '../../core/services/auth.service';

interface DashboardResumen {
  clientesActivos: number;
  ticketsAbiertos: number;
}

@Component({
  selector: 'app-dashboard',
  imports: [],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.css'
})
export class DashboardComponent implements OnInit {
  private readonly http = inject(HttpClient);
  readonly authService = inject(AuthService);

  readonly resumen = signal<DashboardResumen | null>(null);
  readonly cargando = signal(true);
  readonly error = signal<string | null>(null);

  ngOnInit(): void {
    // Llama a un endpoint protegido: si esto responde, el authInterceptor adjuntó el JWT
    // correctamente y el backend lo validó (prueba end-to-end de la Fase 5).
    this.http.get<DashboardResumen>(`${environment.apiUrl}/reportes/dashboard`).subscribe({
      next: (r) => {
        this.resumen.set(r);
        this.cargando.set(false);
      },
      error: () => {
        this.error.set('No se pudo cargar el dashboard.');
        this.cargando.set(false);
      }
    });
  }
}
