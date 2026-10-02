---
name: validar-fase
description: Validar en vivo una fase o cambio de Núcleo antes de darlo por cerrado — API real con los 3 roles, navegador real (CORS, consola, UI por rol), axe y overflow a 375/1280 — y registrar el resultado en docs/TESTING.md. Usar cuando el usuario dice "validemos", "cerremos la fase", "probémoslo en vivo" o al terminar un cambio que toca endpoints o pantallas.
argument-hint: <nombre de la fase o cambio>
---

# Validar fase en vivo

Cambio a validar: `$ARGUMENTS`. El método es el de `docs/TESTING.md` §2: cada fase se valida contra la API viva **y** en navegador real, porque hay bugs que solo el navegador destapa (CORS, `MapInboundClaims` — ver "Hallazgos de Fase 5").

Regla de honestidad: una fila solo lleva ✅ si el caso se ejecutó y dio lo esperado. Lo que no se pudo correr se marca como no ejecutado, con el motivo.

## 1. Delimitar el alcance

Mira qué cambió (`git diff main...HEAD`, `git log`, o los commits de la fase) y arma la tabla de casos **antes** de ejecutar, con el formato de TESTING.md (`| # | Caso | Esperado | Resultado |`). Muéstrasela al usuario. Por cada endpoint tocado incluye como mínimo:

| Caso | Esperado |
|---|---|
| Sin token | 401 |
| Lector lee | 200 |
| Lector escribe (POST/PUT/PATCH/DELETE) | 403 |
| Tecnico/Admin escribe | 2xx |
| Recurso inexistente | 404 ProblemDetails |
| Regla de negocio violada (duplicado, transición inválida, activo de otro cliente…) | 409 con detalle |
| Listados: `?tamano=1000` | `tamanoPagina` = 100 (clamp) |
| Enums en el JSON | nombres (`"EnProgreso"`), nunca enteros |

Y por cada pantalla tocada: carga con datos reales, acciones de escritura visibles solo con `puedeEscribir()`, recarga completa conserva la sesión.

## 2. Precondiciones

1. Con la API **detenida**: `dotnet test backend/Nucleo.Api.Tests/Nucleo.Api.Tests.csproj` en verde.
2. `npm run build --prefix frontend` sin errores.
3. Levanta ambos servidores con las herramientas de preview (nunca con Bash): `preview_start {name: "api"}` y `preview_start {name: "frontend"}`. Si la API no arranca, revisa `preview_logs` — causas típicas: falta `Jwt:Key` en user-secrets, SQLEXPRESS caído, puerto 5112 ocupado.

## 3. Capa API (PowerShell, como en las fases 1–4)

Credenciales: los técnicos demo de `DbSeeder` (`Nucleo123!`) — Admin `diego.torres@nucleo.mx`, Tecnico `carlos.mendez@nucleo.mx`, Lector `sofia.ramirez@nucleo.mx`.

```powershell
$api = 'http://localhost:5112/api'
function Token($email) { (Invoke-RestMethod "$api/auth/login" -Method Post -ContentType 'application/json' -Body (@{ email = $email; password = 'Nucleo123!' } | ConvertTo-Json)).token }
$tLector = Token 'sofia.ramirez@nucleo.mx'
# Para ver el status de un error:
try { Invoke-WebRequest "$api/clientes" -Method Post -Headers @{ Authorization = "Bearer $tLector" } -ContentType 'application/json' -Body '{}' -UseBasicParsing } catch { $_.Exception.Response.StatusCode.value__ }
```

No imprimas los tokens en la respuesta al usuario.

## 4. Navegador real (Browser pane)

En la pestaña del preview `frontend`:

1. Sin sesión, `/` redirige a `/login`.
2. Login con cada rol que el cambio afecte; recorre la pantalla tocada.
3. `read_console_messages` con `onlyErrors` — cero errores. Busca especialmente `NG0100` (valor no derivado de signal en el template; ver `features/tickets/CLAUDE.md`).
4. `read_network_requests` — ninguna llamada a `:5112` fallida por CORS; las peticiones a la API llevan `Authorization`, las de terceros no.
5. Lector: no ve formularios/botones de escritura. Tecnico: no entra a `/admin`.
6. Recarga completa → la sesión persiste.

## 5. Calidad

Para cada ruta tocada:

- **Antes de auditar**, confirma que la app ya renderizó la ruta correcta: `ng serve` sirve una página vacía mientras compila (el primer arranque tarda ~10 s), y auditarla da falsos `document-title`/`html-has-lang`. Comprueba con `javascript_tool` que `document.title` no está vacío, que `document.documentElement.lang === 'es'` y que `location.pathname` es la ruta que querías (los guards redirigen, p. ej. a `/login`). Si el script se corta por una navegación, vuelve a ejecutarlo.
- **axe (WCAG 2 A/AA), objetivo cero violaciones** — con `javascript_tool`:
  ```js
  await new Promise((ok, err) => { if (window.axe) return ok(); const s = document.createElement('script'); s.src = 'https://cdnjs.cloudflare.com/ajax/libs/axe-core/4.10.2/axe.min.js'; s.onload = ok; s.onerror = err; document.head.appendChild(s); });
  (await axe.run(document, { runOnly: ['wcag2a', 'wcag2aa'] })).violations.map(v => ({ id: v.id, nodes: v.nodes.length, target: v.nodes[0]?.target }))
  ```
- **Overflow horizontal** a 375 y 1280 (`resize_window`), recargando tras cada cambio:
  ```js
  document.documentElement.scrollWidth - document.documentElement.clientWidth   // debe ser 0
  ```
  Contenido ancho nuevo va dentro de `.tabla-scroll`, no desbordando el body.
- Vuelve a `resize_window {preset: "desktop"}` al terminar.

Toma un screenshot de la pantalla principal del cambio como evidencia.

## 6. Registrar

Agrega a `docs/TESTING.md` §2 una sección `### <Fase/cambio> (N/N ✅)` con la tabla ejecutada y, si aparecieron bugs, un bloque **Hallazgos** (qué falló, por qué, cómo se arregló). Si algún hallazgo es una lección reusable, llévala también al CLAUDE.md de la carpeta correspondiente.

## 7. Limpiar

`preview_stop` de los servidores que levantaste. Avisa al usuario de los registros que la validación creó en la BD de dev (no los borres sin preguntar).
