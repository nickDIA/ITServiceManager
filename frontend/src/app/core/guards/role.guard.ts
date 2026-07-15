import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { RolTecnico } from '../models/auth.models';
import { AuthService } from '../services/auth.service';

/**
 * Fábrica de guard: roleGuard(['Admin']) restringe la ruta a esos roles.
 * Se usa junto con authGuard (que ya garantiza que hay sesión) en canActivate.
 */
export const roleGuard = (rolesPermitidos: RolTecnico[]): CanActivateFn => {
  return () => {
    const authService = inject(AuthService);
    const router = inject(Router);
    const usuario = authService.usuario();

    return usuario !== null && rolesPermitidos.includes(usuario.rol)
      ? true
      : router.createUrlTree(['/dashboard']);
  };
};
