// PostToolUse (Edit|Write): recordatorio de los espejos backend <-> frontend, que se mantienen a mano.
// También marca el grafo de graphify como pendiente si se tocó código (lo procesa graphify-update.mjs al terminar el turno).
import { writeFileSync, existsSync } from 'node:fs';
import path from 'node:path';
import { leerEntrada, rutaRelativa, raizProyecto, responder } from './lib.mjs';

const entrada = await leerEntrada();
const ruta = rutaRelativa(entrada);
if (!ruta) process.exit(0);

const raiz = raizProyecto(entrada);
const MODELOS_TS = 'frontend/src/app/core/models/nucleo.models.ts';

let aviso = null;
if (/^backend\/Nucleo\.Api\/Domain\/Estado\w*Transiciones\.cs$/.test(ruta)) {
  aviso = `Editaste una máquina de estados. Su espejo es TRANSICIONES_ACTIVO / TRANSICIONES_TICKET en ${MODELOS_TS}: compara la tabla valor por valor y actualiza las pruebas de backend/Nucleo.Api.Tests/Domain/TransicionesTests.cs.`;
} else if (ruta === 'backend/Nucleo.Api/Models/Entities/Enums.cs') {
  aviso = `Editaste los enums. Su espejo son los tipos unión y los arrays (TIPOS_ACTIVO, PRIORIDADES, ESTADOS_TICKET) en ${MODELOS_TS}. Si el enum se persiste, también necesita migración si cambia la longitud (HasMaxLength(20)).`;
} else if (/^backend\/Nucleo\.Api\/Models\/DTOs\/\w+\.cs$/.test(ruta)) {
  aviso = `Editaste un DTO. Su espejo es la interface TS correspondiente en ${MODELOS_TS} (propiedades en camelCase, misma nulabilidad); si cambió un endpoint, revisa también core/services/nucleo-api.service.ts.`;
} else if (ruta === MODELOS_TS) {
  aviso = 'Editaste los modelos espejo del frontend. El backend es la fuente de verdad (Domain/, Models/Entities/Enums.cs, Models/DTOs/): confirma que este cambio lo refleja y no al revés.';
}

// Marca para graphify: solo código, y solo si el grafo existe en este clon.
if (/\.(cs|ts)$/.test(ruta) && existsSync(path.join(raiz, 'graphify-out'))) {
  try { writeFileSync(path.join(raiz, 'graphify-out', '.pendiente'), ruta); } catch { /* sin marca no pasa nada grave */ }
}

if (aviso) {
  responder({ hookSpecificOutput: { hookEventName: 'PostToolUse', additionalContext: `[espejo-frontend] ${aviso}` } });
}
