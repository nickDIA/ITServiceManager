// Stop: si hay cambios sin commitear en las pruebas o en los documentos que citan el total,
// verifica que los cinco totales coinciden entre sí y con el conteo real ([Fact] + filas [InlineData]).
import { execFileSync } from 'node:child_process';
import { readdirSync, readFileSync } from 'node:fs';
import path from 'node:path';
import { leerEntrada, raizProyecto, leer, responder } from './lib.mjs';

const entrada = await leerEntrada();
const raiz = raizProyecto(entrada);

const DOCUMENTOS = [
  ['docs/TESTING.md', /Estado actual: (\d+) pruebas/, '"Estado actual"'],
  ['CLAUDE.md', /# Tests \((\d+) pruebas/, 'sección Commands'],
  ['CLAUDE.md', /the (\d+) Moq-based Service tests/, 'sección Integración continua'],
  ['backend/Nucleo.Api.Tests/CLAUDE.md', /^(\d+) tests \(xUnit \+ Moq\)/m, 'primera línea'],
  ['README.md', /# (\d+) pruebas \(xUnit \+ Moq\)/, 'sección Pruebas y CI'],
];

// Solo si se tocó algo relevante: no molestar en turnos que no tienen que ver con pruebas.
let cambios = '';
try { cambios = execFileSync('git', ['status', '--porcelain'], { cwd: raiz, encoding: 'utf8' }); } catch { process.exit(0); }
const relevante = cambios.split('\n').some((l) => {
  const ruta = l.slice(3).trim().replace(/"/g, '');
  return ruta.startsWith('backend/Nucleo.Api.Tests/') || DOCUMENTOS.some(([doc]) => ruta === doc);
});
if (!relevante) process.exit(0);

const citados = DOCUMENTOS.map(([doc, re, etiqueta]) => {
  const m = leer(raiz, doc)?.match(re);
  return { donde: `${doc} (${etiqueta})`, valor: m ? Number(m[1]) : null };
});

const real = conteoReal(path.join(raiz, 'backend/Nucleo.Api.Tests'));
const problemas = [];
for (const c of citados) {
  if (c.valor === null) problemas.push(`no encontré el total en ${c.donde}`);
  else if (real !== null && c.valor !== real) problemas.push(`${c.donde} dice ${c.valor}`);
}
const valores = new Set(citados.map((c) => c.valor).filter((v) => v !== null));
if (real === null && valores.size > 1) problemas.push(`los documentos no coinciden entre sí: ${[...valores].join(', ')}`);

if (problemas.length) {
  const mensaje = `[conteo-pruebas] Total real: ${real ?? 'no calculable (hay MemberData/ClassData)'}. ${problemas.join('; ')}. ` +
    'Confírmalo con `dotnet test` (línea "Superado: N") y deja las cinco cifras iguales.';
  // stop_hook_active: ya se bloqueó una vez en este ciclo; solo avisar para no entrar en bucle.
  responder(entrada.stop_hook_active ? { systemMessage: mensaje } : { decision: 'block', reason: mensaje });
}

/** [Fact] + una por cada [InlineData]. Devuelve null si hay fuentes de datos que no se pueden contar sin compilar. */
function conteoReal(dir) {
  let total = 0;
  for (const archivo of listarCs(dir)) {
    const texto = readFileSync(archivo, 'utf8');
    if (/\[(MemberData|ClassData)\b/.test(texto)) return null;
    total += (texto.match(/\[Fact\b/g) ?? []).length + (texto.match(/\[InlineData\(/g) ?? []).length;
  }
  return total;
}

function listarCs(dir) {
  const salida = [];
  for (const e of readdirSync(dir, { withFileTypes: true })) {
    if (e.isDirectory()) { if (!['bin', 'obj'].includes(e.name)) salida.push(...listarCs(path.join(dir, e.name))); }
    else if (e.name.endsWith('.cs')) salida.push(path.join(dir, e.name));
  }
  return salida;
}
