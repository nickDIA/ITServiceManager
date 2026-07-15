# Pruebas de Núcleo

Este documento cubre las dos capas de verificación del proyecto:

1. **Suite automatizada** (`backend/Nucleo.Api.Tests`) — xUnit + Moq sobre la capa de Services.
2. **Registro de validación manual por fase** — las pruebas end-to-end ejecutadas contra la API viva y el navegador real al cerrar cada fase.

---

## 1. Suite automatizada

### Cómo correrla

```bash
dotnet test backend/Nucleo.Api.Tests/Nucleo.Api.Tests.csproj
```

> ⚠️ Si la API está corriendo (`dotnet run`), detenla primero: el proceso bloquea
> `Nucleo.Api.exe` y el build de los tests falla con MSB3027.

**Estado actual: 61 pruebas, 61 pasando.**

### Qué se prueba y qué no (diseño de la suite)

- **Se prueba la capa de Services** — donde viven todas las reglas de negocio — con los
  repositorios **mockeados** (Moq). No hay base de datos involucrada: las pruebas verifican
  *lógica*, no SQL.
- **No se mockea BCrypt** en `AuthServiceTests`: verificar el hash es parte de la lógica
  bajo prueba (mockearlo dejaría la prueba sin contenido).
- **Los repositorios no se prueban unitariamente** — son EF Core puro sin lógica; probar
  sus queries requeriría una BD real o un provider en memoria, y eso pertenece a pruebas de
  integración (fuera del alcance actual).
- Las **máquinas de estados** (`Domain/`) se prueban directo como funciones puras, sin mocks:
  sirven de documentación ejecutable de qué transiciones permite cada entidad.

### Cobertura por archivo

#### `Domain/TransicionesTests.cs` (18 pruebas)

| Máquina | Verifica |
|---|---|
| `EstadoActivoTransiciones` | Transiciones permitidas entre Operativo/EnReparacion/EnAlmacen; `Retirado` es terminal (0 salidas); mismo estado no es transición válida. |
| `EstadoTicketTransiciones` | Flujo normal `Abierto→EnProgreso→Resuelto→Cerrado`; escape `Abierto→Cancelado`; no saltar pasos; no reabrir; `Cerrado`/`Cancelado` terminales. |

#### `Services/ClienteServiceTests.cs` (9 pruebas)

| Caso | Regla verificada |
|---|---|
| Crear con RFC duplicado | `ConflictoException`, y **no** se llama a `AgregarAsync`/`GuardarCambiosAsync` |
| Crear válido | RFC normalizado (trim + mayúsculas), nace con `Activo = true` |
| Actualizar inexistente | `RecursoNoEncontradoException` |
| Actualizar con RFC de otro cliente | `ConflictoException` (verifica el parámetro `excluyendoId`) |
| Eliminar con activos asociados | `ConflictoException`, `Eliminar` nunca se llama |
| Eliminar sin activos | `Eliminar` + `GuardarCambiosAsync` exactamente una vez |
| Lecturas | Mapeo entidad→DTO; inexistente devuelve `null` (el controller lo traduce a 404) |

#### `Services/ActivoServiceTests.cs` (11 pruebas) — **el corazón del proyecto**

| Caso | Regla verificada |
|---|---|
| Crear: cliente inexistente / serie duplicada | 404 / 409 respectivamente, sin persistir |
| Crear válido | Nace en `Operativo` (el estado no es input del cliente) |
| Cambio de estado: transición inválida | `ConflictoException` **y `IniciarTransaccionAsync` nunca se llama** — la validación ocurre antes de abrir transacción |
| Cambio de estado válido | **Orden exacto verificado**: `Iniciar → guardar activo → guardar historial → Confirmar`; el historial registra `EstadoAnterior`/`EstadoNuevo`/`TecnicoId` correctos (el técnico viene del parámetro que el controller extrae del JWT, no del DTO) |
| **Rollback (requisito 6)**: la auditoría falla con excepción genérica | `RevertirTransaccionAsync` se llama exactamente una vez, `Confirmar` nunca, la excepción se propaga |
| **Rollback**: la auditoría falla con `SqlException` **547** (violación de FK real de SQL Server, fabricada vía reflexión en `Helpers/SqlExceptionFactory`) | Se traduce a `RecursoNoEncontradoException` (→404) con el id del técnico en el mensaje + rollback |
| `DbUpdateException` que **no** es FK 547 | **No** se traduce a 404: va al catch genérico (rollback + rethrow) — verifica que el filtro `when` discrimina bien |
| Historial | 404 si el activo no existe; mapeo incluye `TecnicoNombre` del join |

#### `Services/TicketServiceTests.cs` (14 pruebas)

| Caso | Regla verificada |
|---|---|
| Crear: cliente/técnico/activo inexistentes | 404 en cada caso |
| Crear con activo de **otro** cliente | `ConflictoException` — la regla de negocio cruzada distintiva de Ticket |
| Crear válido | Nace `Abierto`, `FechaCreacion` asignada, `FechaCierre` null |
| Transiciones inválidas (4 casos) | Saltar pasos, cancelar fuera de `Abierto`, salir de terminales → 409 sin persistir |
| Transición a estado no terminal | `FechaCierre` sigue null |
| Transición a `Cerrado`/`Cancelado` | `FechaCierre` se fija automáticamente |
| Actualizar | El activo se valida contra el `ClienteId` **del ticket** (no contra el DTO, que no trae cliente) |

#### `Services/AuthServiceTests.cs` (4 pruebas)

| Caso | Regla verificada |
|---|---|
| Credenciales válidas | Devuelve token + datos del técnico (BCrypt real verifica el hash) |
| Email inexistente | `CredencialesInvalidasException`; el token nunca se genera |
| Password incorrecta | **La misma excepción** que email inexistente — no filtrar cuál credencial falló |
| Email con espacios | Se recorta antes de buscar |

#### `Services/ReporteServiceTests.cs` (2 pruebas)

| Caso | Regla verificada |
|---|---|
| Dashboard completo | Las 8 métricas del repositorio se mapean campo a campo al DTO |
| Promedio de horas | Redondeo a 1 decimal (`16.666… → 16.7`) |

### Nota sobre `SqlExceptionFactory`

`SqlException` no tiene constructor público (solo lo crea el driver al hablar con SQL Server
real), pero `ActivoService` discrimina el error **547** para traducir la violación de FK a
un 404. El helper `Helpers/SqlExceptionFactory.cs` lo fabrica vía reflexión sobre los
constructores internos de `Microsoft.Data.SqlClient`. Si una actualización del paquete
cambia esas firmas internas, solo fallan los tests que lo usan, con error claro en el helper.

---

## 2. Registro de validación manual por fase

Cada fase se validó contra la API viva (PowerShell/`Invoke-WebRequest`, equivalente a
Postman) antes de darla por cerrada; la Fase 5 se validó además en navegador real.
Credenciales usadas: los 3 técnicos seed (`Nucleo123!`).

### Fase 1 — CRUD de Clientes (8/8 ✅)

| # | Caso | Esperado | Resultado |
|---|---|---|---|
| 1 | `GET /api/clientes/{id}` existente | 200 | ✅ |
| 2 | `GET /api/clientes/999` | 404 ProblemDetails | ✅ |
| 3 | `POST` válido | 201 + header `Location` | ✅ |
| 4 | `POST` RFC duplicado | 409 con detalle | ✅ |
| 5 | `POST` RFC malformado | 400 con error de validación | ✅ |
| 6 | `PUT` actualizar | 204 + verificado con GET | ✅ |
| 7 | `DELETE` cliente con activos | 409 (integridad) | ✅ |
| 8 | `DELETE` cliente sin activos | 204 + GET posterior 404 | ✅ |

### Fase 2 — Activos y transacción central (14/14 ✅)

| # | Caso | Esperado | Resultado |
|---|---|---|---|
| 1-6 | CRUD completo + filtro `?clienteId=` | 200/201/204 | ✅ |
| 7 | `POST` cliente inexistente | 404 | ✅ |
| 8 | `POST` número de serie duplicado | 409 | ✅ |
| 9 | `PATCH estado` transición válida | 200, estado cambiado | ✅ |
| 10 | `GET historial` tras el cambio | 1 entrada con técnico resuelto | ✅ |
| 11 | `PATCH estado` mismo estado | 409 con transiciones permitidas listadas | ✅ |
| 12 | **Rollback**: `tecnicoId` inexistente (9999) a mitad de transacción | 404 | ✅ |
| 13 | **Post-rollback**: estado del activo | Sin cambio (seguía `EnReparacion`) | ✅ |
| 14 | **Post-rollback**: historial | Sin fila fantasma (seguía 1 entrada) | ✅ |

> Nota: desde Fase 3 el caso 12 ya no es reproducible vía API (el técnico sale del JWT,
> no del body). La suite automatizada lo cubre ahora vía `SqlExceptionFactory`.

### Fase 3 — Autenticación y roles (12/12 ✅)

| # | Caso | Esperado | Resultado |
|---|---|---|---|
| 1 | `GET` protegido sin token | 401 | ✅ |
| 2 | Login con password incorrecta | 401 genérico (no filtra si el email existe) | ✅ |
| 3-5 | Login Admin / Tecnico / Lector | 200 con rol correcto en la respuesta | ✅ |
| 6 | `GET` con token Lector | 200 (lectura abierta a todo rol) | ✅ |
| 7 | `POST` con token Lector | 403 (escritura bloqueada) | ✅ |
| 8 | `POST` con token Tecnico | 201 | ✅ |
| 9 | `PATCH estado`: técnico en historial | El del **token**, sin viajar en el body | ✅ |
| 10 | `DELETE` con token Admin | 204 | ✅ |
| 11 | Token manipulado/inválido | 401 | ✅ |
| 12 | Re-verificación tras fix de claims cortos (`MapInboundClaims`) | Roles siguen funcionando | ✅ |

### Fase 4 — Tickets y reportes (17/17 ✅)

| # | Caso | Esperado | Resultado |
|---|---|---|---|
| 1 | `GET /api/tickets` | 6 tickets con cliente+activo+técnico resueltos (3 JOINs) | ✅ |
| 2 | Filtro `?estado=Abierto` | 2 | ✅ |
| 3 | `POST` válido | 201, nace `Abierto` | ✅ |
| 4 | `POST` activo de otro cliente | 409 | ✅ |
| 5 | `POST` cliente inexistente | 404 | ✅ |
| 6-8 | `Abierto→EnProgreso→Resuelto→Cerrado` | 200 en cada paso | ✅ |
| 9 | `EnProgreso→Cancelado` | 409 con transiciones permitidas | ✅ |
| 10 | `FechaCierre` | Solo se fija al llegar a terminal | ✅ |
| 11-14 | Dashboard: GROUP BY ×2, SUM, AVG, subconsulta | Números exactos vs seed (19000, 15, 1 cliente sin tickets abiertos…) | ✅ |
| 15-17 | Roles en Tickets/Reportes (Lector lee, no escribe; sin token 401) | 200/403/401 | ✅ |

### Fase 5 — Frontend Angular (12/12 ✅, navegador real)

| # | Caso | Esperado | Resultado |
|---|---|---|---|
| 1 | Visitar `/` sin sesión | Redirige a `/login` (authGuard) | ✅ |
| 2 | Login con password incorrecta | Banner "Email o contraseña incorrectos." | ✅ |
| 3-5 | Login Admin / Tecnico / Lector | Redirige a `/dashboard` con nombre y rol | ✅ |
| 6 | Dashboard | Carga métricas reales vía JWT (interceptor end-to-end) | ✅ |
| 7 | `/admin` como Tecnico | Rebotado a `/dashboard` (roleGuard) | ✅ |
| 8 | `/admin` como Lector | Rebotado a `/dashboard` | ✅ |
| 9 | `/admin` como Admin | Acceso permitido | ✅ |
| 10 | Recarga completa de página con sesión | Sesión persiste (rehidratada del JWT en localStorage) | ✅ |
| 11 | Cerrar sesión | Token eliminado + redirige a `/login` | ✅ |
| 12 | Lazy loading | `login`/`dashboard`/`admin` en chunks separados del bundle inicial | ✅ |

**Hallazgos de Fase 5** (bugs reales que el navegador destapó y Postman no podía):
1. **CORS ausente** — el preflight `OPTIONS` devolvía 405; se agregó `AddCors`/`UseCors` para `http://localhost:4200`.
2. **`MapInboundClaims`** — el `JwtBearerHandler` remapeaba `sub`/`role` a URIs de WS-Federation al validar, rompiendo `[Authorize(Roles=…)]` con 403 silencioso; se desactivó en `AddJwtBearer`.

---

## Pendientes de prueba (ideas futuras)

- **Pruebas de integración** con la BD real (o Testcontainers): queries de repositorios,
  transacción real de `CambiarEstadoAsync` contra SQL Server, y el seeder idempotente.
- **Pruebas de `TokenService`**: firma y claims del JWT generado (decodificarlo y verificar
  `sub`/`role`/`exp`).
- **Frontend**: specs de `AuthService`/guards con `TestBed` (hoy solo existe el spec básico
  de `AppComponent`), y pruebas e2e (Playwright) del flujo de login.
