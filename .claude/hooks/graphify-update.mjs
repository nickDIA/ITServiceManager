// Stop (async): si en este turno se editó código (marca graphify-out/.pendiente), actualiza el grafo una sola vez.
// Una vez por turno y no por edición: diez ediciones seguidas no deben lanzar diez `graphify update` en paralelo.
import { existsSync, rmSync } from 'node:fs';
import { spawnSync } from 'node:child_process';
import path from 'node:path';
import { leerEntrada, raizProyecto } from './lib.mjs';

const entrada = await leerEntrada();
const raiz = raizProyecto(entrada);
const marca = path.join(raiz, 'graphify-out', '.pendiente');

if (existsSync(marca)) {
  rmSync(marca, { force: true }); // se borra antes: si se edita algo mientras corre, la próxima vuelta lo recoge.
  // Comando fijo como una sola cadena: shell:true hace falta en Windows para resolver graphify(.exe/.cmd) en el PATH.
  spawnSync('graphify update .', { cwd: raiz, shell: true, stdio: 'ignore', timeout: 120_000 });
}
