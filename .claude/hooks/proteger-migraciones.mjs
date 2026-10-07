// PreToolUse (Edit|Write): editar Data/Migrations a mano requiere confirmación del usuario.
// "ask" y no "deny": la skill /nueva-migracion sí edita el .cs generado en un caso legítimo
// (cambiar un DropColumn+AddColumn por RenameColumn), y eso debe poder hacerse con permiso.
import { leerEntrada, rutaRelativa, responder } from './lib.mjs';

const entrada = await leerEntrada();
const ruta = rutaRelativa(entrada);

if (ruta && /^backend\/Nucleo\.Api\/Data\/Migrations\//i.test(ruta)) {
  const esSnapshot = /ModelSnapshot\.cs$/i.test(ruta);
  responder({
    hookSpecificOutput: {
      hookEventName: 'PreToolUse',
      permissionDecision: 'ask',
      permissionDecisionReason: esSnapshot
        ? 'AppDbContextModelSnapshot.cs lo genera EF: editarlo a mano desincroniza el modelo y la próxima migración saldrá mal. Usa la skill /nueva-migracion.'
        : 'Las migraciones las genera `dotnet ef` (skill /nueva-migracion). Editarlas a mano solo es válido para corregir el .cs recién generado (p. ej. DropColumn→RenameColumn) antes de aplicarlo. ¿Aprobar esta edición?',
    },
  });
}
