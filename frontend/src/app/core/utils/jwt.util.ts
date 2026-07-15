/**
 * Decodifica el payload (segunda parte) de un JWT sin validar la firma —solo para leer
 * claims en el cliente y armar el estado de sesión al recargar la página. La validación
 * real (firma, expiración, issuer/audience) siempre la hace el backend en cada request.
 */
export function decodeJwtPayload<T>(token: string): T | null {
  try {
    const base64Url = token.split('.')[1];
    const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
    const json = decodeURIComponent(
      atob(base64)
        .split('')
        .map((c) => '%' + c.charCodeAt(0).toString(16).padStart(2, '0'))
        .join('')
    );
    return JSON.parse(json) as T;
  } catch {
    return null;
  }
}
