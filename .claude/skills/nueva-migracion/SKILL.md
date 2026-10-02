---
name: nueva-migracion
description: Crear, revisar y aplicar una migración de EF Core en Núcleo (backend/Nucleo.Api). Usar cuando se agrega o cambia una entidad, propiedad, relación/FK, índice o enum en Models/Entities o Data/Configurations, o cuando el usuario pide "una migración".
argument-hint: <NombreMigracionPascalCase>
---

# Nueva migración de EF Core

Nombre de la migración: `$ARGUMENTS` (PascalCase, en español, describe el cambio — p. ej. `IndicesRendimiento`, `AgregarContratoMoneda`). Si viene vacío, propón uno y confírmalo.

Las reglas del modelo están en `backend/Nucleo.Api/Data/CLAUDE.md` — léelo antes de empezar si no está cargado.

## 1. Revisar el cambio de modelo ANTES de generar

Revisa el diff de `Models/Entities/` y `Data/Configurations/` y comprueba, para cada punto que aplique:

- **FK nueva** → `OnDelete` explícito en su `IEntityTypeConfiguration`. Nunca dejes el `Cascade` por defecto sin trazar las rutas: SQL Server rechaza "multiple cascade paths". Hoy solo `Activo → HistorialActivo` es `Cascade`; `Cliente → *` y `Tecnico → HistorialActivo` son `Restrict`; `Ticket → Activo` (nullable) es `SetNull`.
- **Enum nuevo/cambiado** → `.HasConversion<string>().HasMaxLength(20)` (si no, se guarda como `int` o `nvarchar(max)`). Recuerda el espejo TS en `frontend/src/app/core/models/nucleo.models.ts`.
- **Columna que se usará en `WHERE`/`ORDER BY`/`GROUP BY`** → `HasIndex` en la configuración. Las FK ya las indexa EF; lo que falta suele ser la columna de filtro u orden (lección de las pruebas de carga).
- **Columna no-nullable en tabla con datos** → define `HasDefaultValue`/valor por defecto o la migración fallará con las filas existentes (incluidos los datos de carga `seed-bulk`).
- **Datos semilla** → si la tabla nueva necesita demo, agrega un bloque en `DbSeeder` con **su propio** `AnyAsync`, no lo cuelgues de otra tabla.

Si algo no cuadra, para y pregúntale al usuario antes de generar.

## 2. Precondición: la API no debe estar corriendo

`dotnet ef` compila el proyecto y el `Nucleo.Api.exe` en ejecución bloquea la salida. Comprueba:

```powershell
Get-Process Nucleo.Api -ErrorAction SilentlyContinue
```

Si hay proceso, pide detenerlo (o `preview_stop` si lo levantaste tú con la config `api`). Luego `dotnet build backend/Nucleo.Api/Nucleo.Api.csproj` para fallar rápido por errores de compilación.

## 3. Generar

```
dotnet ef migrations add $ARGUMENTS --project backend/Nucleo.Api/Nucleo.Api.csproj --startup-project backend/Nucleo.Api/Nucleo.Api.csproj -o Data/Migrations
```

## 4. Revisar el archivo generado (no saltarse esto)

Lee `Data/Migrations/<timestamp>_$ARGUMENTS.cs` completo y busca:

- `DropColumn`/`DropTable` inesperados — EF interpreta un **renombre** como drop + add y pierde datos; si fue renombre, cámbialo a `RenameColumn`/`RenameTable`.
- `onDelete: ReferentialAction.Cascade` donde no lo esperabas.
- `nvarchar(max)` en una columna de enum (falta `HasMaxLength`).
- `CreateIndex` presente para cada índice que pretendías.
- `Down()` coherente con `Up()`.

Resume al usuario qué hace la migración en 3–5 líneas antes de aplicarla.

## 5. Aplicar y verificar

En Development basta con arrancar la API (`Program.cs` corre `MigrateAsync()`), o explícitamente:

```
dotnet ef database update --project backend/Nucleo.Api/Nucleo.Api.csproj --startup-project backend/Nucleo.Api/Nucleo.Api.csproj
```

Verifica contra la BD real:

```
sqlcmd -S .\SQLEXPRESS -E -C -d NucleoDb -Q "SET NOCOUNT ON; SELECT TOP 3 MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId DESC"
```

(`-C` confía en el certificado autofirmado de la instancia local — sin él, el driver ODBC 18 rechaza la conexión por SSL.) Y, si agregaste índices, que existan (las tablas están en plural: `Tickets`, `Activos`…):

```
sqlcmd -S .\SQLEXPRESS -E -C -d NucleoDb -Q "SET NOCOUNT ON; SELECT name FROM sys.indexes WHERE object_id = OBJECT_ID('<Tabla>') AND name IS NOT NULL"
```

## 6. Cerrar

- `dotnet test backend/Nucleo.Api.Tests/Nucleo.Api.Tests.csproj` (con la API detenida).
- Commitea juntos la migración, su `.Designer.cs` y `AppDbContextModelSnapshot.cs`.
- Si la migración enseña algo nuevo (un índice con su porqué, una cascada evitada), anótalo en `backend/Nucleo.Api/Data/CLAUDE.md`.
- `graphify update .`

## Deshacer

- **No aplicada todavía:** `dotnet ef migrations remove --project ... --startup-project ...` (mismos flags).
- **Ya aplicada:** primero `dotnet ef database update <MigracionAnterior> ...`, luego `migrations remove`. Nunca borres el `.cs` a mano: deja el snapshot inconsistente.
