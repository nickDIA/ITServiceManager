import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  Activo, ActualizarCliente, Cliente, CrearActivo, CrearCliente, CrearTicket,
  EstadoActivo, EstadoTicket, HistorialActivo, ReporteDashboard, Tecnico, Ticket
} from '../models/nucleo.models';

/**
 * Cliente HTTP de la API de Núcleo. Devuelve Observables crudos: cada componente decide
 * si consume con async pipe (listas) o con subscribe (acciones puntuales).
 */
@Injectable({ providedIn: 'root' })
export class NucleoApiService {
  private readonly http = inject(HttpClient);
  private readonly api = environment.apiUrl;

  // ------------------------------------------------ Clientes
  obtenerClientes(): Observable<Cliente[]> {
    return this.http.get<Cliente[]>(`${this.api}/clientes`);
  }

  crearCliente(dto: CrearCliente): Observable<Cliente> {
    return this.http.post<Cliente>(`${this.api}/clientes`, dto);
  }

  actualizarCliente(id: number, dto: ActualizarCliente): Observable<void> {
    return this.http.put<void>(`${this.api}/clientes/${id}`, dto);
  }

  eliminarCliente(id: number): Observable<void> {
    return this.http.delete<void>(`${this.api}/clientes/${id}`);
  }

  // ------------------------------------------------ Activos
  obtenerActivos(clienteId?: number | null): Observable<Activo[]> {
    let params = new HttpParams();
    if (clienteId) params = params.set('clienteId', clienteId);
    return this.http.get<Activo[]>(`${this.api}/activos`, { params });
  }

  crearActivo(dto: CrearActivo): Observable<Activo> {
    return this.http.post<Activo>(`${this.api}/activos`, dto);
  }

  eliminarActivo(id: number): Observable<void> {
    return this.http.delete<void>(`${this.api}/activos/${id}`);
  }

  cambiarEstadoActivo(id: number, nuevoEstado: EstadoActivo, motivo: string): Observable<Activo> {
    return this.http.patch<Activo>(`${this.api}/activos/${id}/estado`, { nuevoEstado, motivo });
  }

  obtenerHistorialActivo(id: number): Observable<HistorialActivo[]> {
    return this.http.get<HistorialActivo[]>(`${this.api}/activos/${id}/historial`);
  }

  // ------------------------------------------------ Tickets
  obtenerTickets(): Observable<Ticket[]> {
    return this.http.get<Ticket[]>(`${this.api}/tickets`);
  }

  crearTicket(dto: CrearTicket): Observable<Ticket> {
    return this.http.post<Ticket>(`${this.api}/tickets`, dto);
  }

  cambiarEstadoTicket(id: number, nuevoEstado: EstadoTicket): Observable<Ticket> {
    return this.http.patch<Ticket>(`${this.api}/tickets/${id}/estado`, { nuevoEstado });
  }

  eliminarTicket(id: number): Observable<void> {
    return this.http.delete<void>(`${this.api}/tickets/${id}`);
  }

  // ------------------------------------------------ Técnicos y reportes
  obtenerTecnicos(): Observable<Tecnico[]> {
    return this.http.get<Tecnico[]>(`${this.api}/tecnicos`);
  }

  obtenerDashboard(): Observable<ReporteDashboard> {
    return this.http.get<ReporteDashboard>(`${this.api}/reportes/dashboard`);
  }
}
