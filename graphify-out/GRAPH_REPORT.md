# Graph Report - .  (2026-07-16)

## Corpus Check
- Corpus is ~45,690 words - fits in a single context window. You may not need a graph.

## Summary
- 977 nodes · 1897 edges · 48 communities (41 shown, 7 thin omitted)
- Extraction: 95% EXTRACTED · 5% INFERRED · 0% AMBIGUOUS · INFERRED: 89 edges (avg confidence: 0.8)
- Token cost: 0 input · 144,104 output

## Community Hubs (Navigation)
- Tickets API Endpoints
- Auth Errors & Exceptions
- Activo DTOs & Estado
- Clientes API Endpoints
- Login & Auth Flow
- vexp Index Manifest
- Angular CLI Build Config
- Architecture Rationale (CLAUDE.md)
- Historial de Activo (Auditoría)
- Dashboard Reportes API
- Activos API Endpoints
- EF Core Entity Configurations
- Frontend Auth Guards & Models
- DbContext & Seeding
- Angular Core Dependencies
- Frontend DTO Mirrors
- Frontend API Client Service
- Tecnicos Endpoint (solo lectura)
- Backend Project & NuGet Packages
- Angular Dev/Test Tooling
- EF Core Migrations
- Launch Profiles Config
- App Shell & Confirm Dialog
- Clientes Feature UI
- Activos Feature UI
- Reporte Aggregation Queries
- npm Scripts
- Global Exception Handler
- vexp Manifest Metadata
- vexp Pipeline Tooling
- Tickets Feature UI
- Nucleo Project Overview (CLAUDE.md)
- SQL Exception Test Helper
- vexp Guard Hook
- Dashboard Feature UI
- EF Core Conventions Rationale
- Angular Compiler CLI Dep
- tslib Dependency
- Karma-Jasmine Dependency
- Jasmine Type Definitions
- Dev Environment Config

## God Nodes (most connected - your core abstractions)
1. `Nucleo.Api.Models.Entities` - 54 edges
2. `file_hashes` - 50 edges
3. `Nucleo.Api.Models.DTOs` - 38 edges
4. `Nucleo.Api.Repositories` - 28 edges
5. `Nucleo.Api.Services` - 26 edges
6. `EstadoTicket` - 21 edges
7. `NucleoApiService` - 21 edges
8. `AppDbContext` - 19 edges
9. `Activo` - 19 edges
10. `AuthService` - 19 edges

## Surprising Connections (you probably didn't know these)
- `Angular CLI Generated Boilerplate Docs` --semantically_similar_to--> `Angular 19 Standalone Frontend Architecture (Fase 5-6)`  [INFERRED] [semantically similar]
  frontend/README.md → CLAUDE.md
- `Nucleo.Api.Tests Build Output Manifest` --conceptually_related_to--> `Automated xUnit+Moq Test Suite`  [INFERRED]
  backend/Nucleo.Api.Tests/obj/Debug/net10.0/Nucleo.Api.Tests.csproj.FileListAbsolute.txt → docs/TESTING.md
- `Nucleo.Api Build Output Manifest` --conceptually_related_to--> `Núcleo IT Asset Management Platform`  [INFERRED]
  backend/Nucleo.Api/obj/Debug/net10.0/Nucleo.Api.csproj.FileListAbsolute.txt → CLAUDE.md
- `LoginComponent Template` --conceptually_related_to--> `JWT Bearer Auth (Fase 3)`  [EXTRACTED]
  frontend/src/app/features/login/login.component.html → CLAUDE.md
- `AdminComponent Template` --conceptually_related_to--> `Role-Based UI Is UX-Only (API 403 Is Real Enforcement)`  [EXTRACTED]
  frontend/src/app/features/admin/admin.component.html → CLAUDE.md

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Fase 6 Signal-Based Frontend Patterns** — claude_frontend_architecture, claude_async_pipe_pattern, claude_effect_pattern, claude_computed_pattern, claude_role_based_ui [EXTRACTED 1.00]
- **Automated Test Suite Coverage Group** — docs_testing_test_suite, docs_testing_transicionestests, docs_testing_clienteservicetests, docs_testing_activoservicetests, docs_testing_ticketservicetests, docs_testing_authservicetests, docs_testing_reporteservicetests [EXTRACTED 1.00]
- **JWT Auth Flow Across Backend and Frontend** — claude_jwt_auth, claude_mapinboundclaims, claude_auth_interceptor_ts, claude_auth_guard_role_guard, frontend_src_app_features_login_login_component_template [EXTRACTED 1.00]

## Communities (48 total, 7 thin omitted)

### Community 0 - "Tickets API Endpoints"
Cohesion: 0.05
Nodes (54): ActionResult, Authorize, CancellationToken, HttpDelete, HttpGet, HttpPatch, HttpPost, HttpPut (+46 more)

### Community 1 - "Auth Errors & Exceptions"
Cohesion: 0.06
Nodes (17): ConflictoException, CredencialesInvalidasException, RecursoNoEncontradoException, string, Roles, Nucleo.Api.Tests.Services, Nucleo.Api.Data, Nucleo.Api.Repositories (+9 more)

### Community 2 - "Activo DTOs & Estado"
Cohesion: 0.07
Nodes (38): IReadOnlyList, DateTime, ActivoResponseDto, DateTime, ActualizarActivoDto, CambiarEstadoActivoDto, DateTime, CrearActivoDto (+30 more)

### Community 3 - "Clientes API Endpoints"
Cohesion: 0.07
Nodes (35): ActionResult, Authorize, CancellationToken, HttpDelete, HttpGet, HttpPost, HttpPut, IActionResult (+27 more)

### Community 4 - "Login & Auth Flow"
Cohesion: 0.06
Nodes (38): ActionResult, CancellationToken, HttpPost, ProducesResponseType, Task, AuthController, LoginDto, DateTime (+30 more)

### Community 5 - "vexp Index Manifest"
Cohesion: 0.04
Nodes (50): file_hashes, backend/Nucleo.Api/Common/Exceptions/ConflictoException.cs, backend/Nucleo.Api/Common/Exceptions/RecursoNoEncontradoException.cs, backend/Nucleo.Api/Common/GlobalExceptionHandler.cs, backend/Nucleo.Api/Controllers/ActivosController.cs, backend/Nucleo.Api/Controllers/ClientesController.cs, backend/Nucleo.Api/Data/AppDbContext.cs, backend/Nucleo.Api/Data/Configurations/ActivoConfiguration.cs (+42 more)

### Community 6 - "Angular CLI Build Config"
Cohesion: 0.05
Nodes (48): build, extract-i18n, serve, test, builder, configurations, defaultConfiguration, options (+40 more)

### Community 7 - "Architecture Rationale (CLAUDE.md)"
Cohesion: 0.07
Nodes (40): Nucleo.Api.Tests Build Output Manifest, ActivoService.CambiarEstadoAsync Reference Pattern, Async Pipe Pattern (features/clientes), authGuard / roleGuard Route Guards, auth.interceptor.ts JWT Attachment, auth.service.ts Signal-Based Session State, Computed Signals Pattern (tickets board, dashboard), CORS Configuration for Frontend Origin (+32 more)

### Community 8 - "Historial de Activo (Auditoría)"
Cohesion: 0.08
Nodes (19): DateTime, HistorialActivo, CancellationToken, IReadOnlyList, Task, HistorialActivoRepositorio, CancellationToken, IReadOnlyList (+11 more)

### Community 9 - "Dashboard Reportes API"
Cohesion: 0.10
Nodes (23): ActionResult, CancellationToken, HttpGet, ProducesResponseType, Task, ReportesController, IReadOnlyDictionary, ReporteDashboardDto (+15 more)

### Community 10 - "Activos API Endpoints"
Cohesion: 0.17
Nodes (17): ActionResult, Authorize, CancellationToken, HttpDelete, HttpGet, HttpPatch, HttpPost, HttpPut (+9 more)

### Community 11 - "EF Core Entity Configurations"
Cohesion: 0.08
Nodes (16): EntityTypeBuilder, ActivoConfiguration, EntityTypeBuilder, ClienteConfiguration, EntityTypeBuilder, ContratoConfiguration, EntityTypeBuilder, HistorialActivoConfiguration (+8 more)

### Community 12 - "Frontend Auth Guards & Models"
Cohesion: 0.13
Nodes (14): authGuard(), roleGuard(), LoginRequest, LoginResponse, UsuarioActual, AuthService, JwtPayload, Injectable (+6 more)

### Community 13 - "DbContext & Seeding"
Cohesion: 0.16
Nodes (14): DbSet, ModelBuilder, AppDbContext, CancellationToken, string, Task, DbSeeder, CancellationToken (+6 more)

### Community 14 - "Angular Core Dependencies"
Cohesion: 0.11
Nodes (19): @angular/common, @angular/compiler, @angular/core, @angular/forms, @angular/platform-browser, @angular/platform-browser-dynamic, @angular/router, dependencies (+11 more)

### Community 15 - "Frontend DTO Mirrors"
Cohesion: 0.22
Nodes (15): RolTecnico, ActualizarCliente, CrearActivo, CrearCliente, CrearTicket, EstadoActivo, ESTADOS_TICKET, HistorialActivo (+7 more)

### Community 16 - "Frontend API Client Service"
Cohesion: 0.13
Nodes (5): EstadoTicket, ReporteDashboard, Ticket, NucleoApiService, Injectable

### Community 17 - "Tecnicos Endpoint (solo lectura)"
Cohesion: 0.12
Nodes (15): ActionResult, CancellationToken, HttpGet, IReadOnlyList, ProducesResponseType, Task, TecnicosController, TecnicoResponseDto (+7 more)

### Community 18 - "Backend Project & NuGet Packages"
Cohesion: 0.12
Nodes (15): net10.0, net10.0, BCrypt.Net-Next (4.2.0), coverlet.collector (6.0.4), Microsoft.AspNetCore.Authentication.JwtBearer (10.0.0), Microsoft.AspNetCore.OpenApi (10.0.7), Microsoft.EntityFrameworkCore.Design (10.0.9), Microsoft.EntityFrameworkCore.SqlServer (10.0.9) (+7 more)

### Community 19 - "Angular Dev/Test Tooling"
Cohesion: 0.12
Nodes (17): @angular/cli, @angular-devkit/build-angular, devDependencies, @angular/cli, @angular-devkit/build-angular, jasmine-core, karma, karma-chrome-launcher (+9 more)

### Community 20 - "EF Core Migrations"
Cohesion: 0.13
Nodes (9): ModelBuilder, InicialNucleo, InicialNucleo, ModelBuilder, AppDbContextModelSnapshot, Nucleo.Api.Data.Migrations, Migration, MigrationBuilder (+1 more)

### Community 21 - "Launch Profiles Config"
Cohesion: 0.13
Nodes (15): ASPNETCORE_ENVIRONMENT, applicationUrl, commandName, dotnetRunMessages, environmentVariables, launchBrowser, applicationUrl, commandName (+7 more)

### Community 22 - "App Shell & Confirm Dialog"
Cohesion: 0.17
Nodes (8): AppComponent, Component, appConfig, routes, authInterceptor(), ConfirmDialogComponent, Component, HostListener

### Community 23 - "Clientes Feature UI"
Cohesion: 0.20
Nodes (6): Cliente, ClientesComponent, Component, ConfirmDialogService, EstadoConfirmacion, Injectable

### Community 24 - "Activos Feature UI"
Cohesion: 0.20
Nodes (3): Activo, ActivosComponent, Component

### Community 25 - "Reporte Aggregation Queries"
Cohesion: 0.41
Nodes (4): CancellationToken, IReadOnlyDictionary, Task, ReporteRepositorio

### Community 26 - "npm Scripts"
Cohesion: 0.20
Nodes (9): name, private, scripts, build, ng, start, test, watch (+1 more)

### Community 27 - "Global Exception Handler"
Cohesion: 0.22
Nodes (8): CancellationToken, Exception, ValueTask, GlobalExceptionHandler, HttpContext, IExceptionHandler, ILogger, IProblemDetailsService

### Community 28 - "vexp Manifest Metadata"
Cohesion: 0.22
Nodes (8): indexed_at_commit, indexed_at_timestamp, schema_version, stats, total_edges, total_files, total_nodes, vexp_version

### Community 29 - "vexp Pipeline Tooling"
Cohesion: 0.40
Nodes (6): expand_vexp_ref Tool, get_skeleton Tool, index_status Tool, PreToolUse Hook Blocks Grep/Glob, run_pipeline Tool, vexp Context-Aware AI Coding Pipeline

### Community 31 - "Nucleo Project Overview (CLAUDE.md)"
Cohesion: 0.40
Nodes (5): Nucleo.Api Build Output Manifest, IRepositorio<T> Open Generic Repository, Controller-Service-Repository-DbContext Layered Architecture, Núcleo IT Asset Management Platform, Project Phases Fase 1-6

### Community 32 - "SQL Exception Test Helper"
Cohesion: 0.40
Nodes (3): SqlExceptionFactory, Nucleo.Api.Tests.Helpers, SqlException

### Community 33 - "vexp Guard Hook"
Cohesion: 0.83
Nodes (3): vexp-guard.sh script, vexp_allow(), vexp_deny()

### Community 35 - "EF Core Conventions Rationale"
Cohesion: 0.67
Nodes (3): DbSeeder Idempotent Seeding, Mixed Delete Behaviors Avoid Cascade Path Conflicts, EF Core Model Conventions

## Knowledge Gaps
- **159 isolated node(s):** `.claude/CLAUDE.md`, `.claude/hooks/vexp-guard.sh`, `backend/Nucleo.Api/Common/Exceptions/ConflictoException.cs`, `backend/Nucleo.Api/Common/Exceptions/RecursoNoEncontradoException.cs`, `backend/Nucleo.Api/Common/GlobalExceptionHandler.cs` (+154 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **7 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `Nucleo.Api.Models.Entities` connect `Auth Errors & Exceptions` to `Tickets API Endpoints`, `Activo DTOs & Estado`, `Clientes API Endpoints`, `Login & Auth Flow`, `Historial de Activo (Auditoría)`, `EF Core Entity Configurations`?**
  _High betweenness centrality (0.072) - this node is a cross-community bridge._
- **Why does `Nucleo.Api.Models.DTOs` connect `Auth Errors & Exceptions` to `Clientes API Endpoints`, `Login & Auth Flow`?**
  _High betweenness centrality (0.038) - this node is a cross-community bridge._
- **Why does `AppDbContext` connect `DbContext & Seeding` to `Tickets API Endpoints`, `Auth Errors & Exceptions`, `Activo DTOs & Estado`, `Clientes API Endpoints`, `Login & Auth Flow`, `Historial de Activo (Auditoría)`, `EF Core Entity Configurations`, `Reporte Aggregation Queries`?**
  _High betweenness centrality (0.037) - this node is a cross-community bridge._
- **What connects `.claude/CLAUDE.md`, `.claude/hooks/vexp-guard.sh`, `backend/Nucleo.Api/Common/Exceptions/ConflictoException.cs` to the rest of the system?**
  _159 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Tickets API Endpoints` be split into smaller, more focused modules?**
  _Cohesion score 0.05191919191919192 - nodes in this community are weakly interconnected._
- **Should `Auth Errors & Exceptions` be split into smaller, more focused modules?**
  _Cohesion score 0.058747160012982795 - nodes in this community are weakly interconnected._
- **Should `Activo DTOs & Estado` be split into smaller, more focused modules?**
  _Cohesion score 0.06516105146242132 - nodes in this community are weakly interconnected._