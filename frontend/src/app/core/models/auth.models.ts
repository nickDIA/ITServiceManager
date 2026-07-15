export type RolTecnico = 'Admin' | 'Tecnico' | 'Lector';

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  expiraEn: string;
  tecnicoId: number;
  nombre: string;
  email: string;
  rol: RolTecnico;
}

export interface UsuarioActual {
  tecnicoId: number;
  nombre: string;
  email: string;
  rol: RolTecnico;
}
