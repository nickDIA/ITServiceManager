// PreToolUse (Bash|PowerShell): bloquea dotnet build/test/ef/run mientras Nucleo.Api está corriendo.
// Sin esto el comando falla igual, pero con un error de archivo bloqueado (MSB3027/MSB3021) poco claro.
import { execFileSync } from 'node:child_process';
import { leerEntrada, responder } from './lib.mjs';

const entrada = await leerEntrada();
const comando = entrada?.tool_input?.command ?? '';

// Por segmento, para no disparar con un "dotnet test" que solo aparece dentro de un mensaje de commit.
const segmentos = comando.split(/&&|\|\||;|\||\r?\n/);
const compila = segmentos.some((s) =>
  /^\s*(timeout\s+\d+\s+)?dotnet(\.exe)?\s+(build|test|ef|run|publish|watch)\b/i.test(s));

if (compila && apiCorriendo()) {
  responder({
    hookSpecificOutput: {
      hookEventName: 'PreToolUse',
      permissionDecision: 'deny',
      permissionDecisionReason:
        'Nucleo.Api está corriendo y bloquea los archivos de compilación: este comando fallaría con un error de archivo bloqueado. ' +
        'Detén la API primero (preview_stop del servidor "api", o pídeselo al usuario si la levantó él) y vuelve a ejecutarlo.',
    },
  });
}

function apiCorriendo() {
  try {
    if (process.platform === 'win32') {
      const salida = execFileSync('tasklist', ['/FI', 'IMAGENAME eq Nucleo.Api.exe', '/FO', 'CSV', '/NH'], { encoding: 'utf8' });
      return salida.includes('"Nucleo.Api.exe"');
    }
    execFileSync('pgrep', ['-f', 'Nucleo.Api'], { stdio: 'ignore' });
    return true;
  } catch {
    return false; // pgrep sin coincidencias, o no se pudo consultar: no bloquear por las dudas.
  }
}
