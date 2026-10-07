// Utilidades compartidas por los hooks de Núcleo (ver .claude/settings.json).
import { readFileSync } from 'node:fs';
import path from 'node:path';

/** Lee el JSON que Claude Code manda por stdin. Nunca lanza: un hook roto no debe trabar la sesión. */
export async function leerEntrada() {
  let datos = '';
  for await (const trozo of process.stdin) datos += trozo;
  try { return JSON.parse(datos || '{}'); } catch { return {}; }
}

/** Raíz del repo: la que da Claude Code, o el cwd del evento como respaldo. */
export function raizProyecto(entrada) {
  return process.env.CLAUDE_PROJECT_DIR || entrada.cwd || process.cwd();
}

/** Ruta del archivo de una herramienta Edit/Write, relativa a la raíz y con `/`. */
export function rutaRelativa(entrada) {
  const ruta = entrada?.tool_input?.file_path;
  if (!ruta) return null;
  return path.relative(raizProyecto(entrada), ruta).replace(/\\/g, '/');
}

export function leer(raiz, relativa) {
  try { return readFileSync(path.join(raiz, relativa), 'utf8'); } catch { return null; }
}

export function responder(objeto) {
  process.stdout.write(JSON.stringify(objeto));
}
