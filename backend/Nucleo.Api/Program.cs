using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Nucleo.Api.Common;
using Nucleo.Api.Data;
using Nucleo.Api.Repositories;
using Nucleo.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// ----------------------------------------------------------------------------
// 1. MVC / Controllers
//    Enums como string en JSON (p. ej. "EnReparacion" en vez de 1): más legible en
//    Postman y consistente con cómo se guardan los enums en la BD (HasConversion<string>).
// ----------------------------------------------------------------------------
builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// ----------------------------------------------------------------------------
// 2. Swagger / OpenAPI (UI para probar endpoints en el navegador, además de Postman)
// ----------------------------------------------------------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ----------------------------------------------------------------------------
// 3. EF Core / SQL Server
//    El DbContext se registra con ciclo de vida Scoped (uno por request HTTP).
// ----------------------------------------------------------------------------
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("NucleoDb")));

// ----------------------------------------------------------------------------
// 4. Repositorios
//    - Registro de genérico ABIERTO: cualquier IRepositorio<T> se resuelve a Repositorio<T>.
//    - Repos específicos: se registran aparte para inyectar la interfaz especializada.
// ----------------------------------------------------------------------------
builder.Services.AddScoped(typeof(IRepositorio<>), typeof(Repositorio<>));
builder.Services.AddScoped<IClienteRepositorio, ClienteRepositorio>();
builder.Services.AddScoped<IActivoRepositorio, ActivoRepositorio>();
builder.Services.AddScoped<IHistorialActivoRepositorio, HistorialActivoRepositorio>();
builder.Services.AddScoped<ITecnicoRepositorio, TecnicoRepositorio>();
builder.Services.AddScoped<ITicketRepositorio, TicketRepositorio>();

// Reportes: no mapea a una sola entidad (cruza Cliente/Activo/Ticket/Contrato), por eso
// no es un IRepositorio<T> específico como los demás.
builder.Services.AddScoped<IReporteRepositorio, ReporteRepositorio>();

// Unit of Work: da control transaccional explícito al Service (ver ActivoService.CambiarEstadoAsync).
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// ----------------------------------------------------------------------------
// 5. Servicios de dominio
// ----------------------------------------------------------------------------
builder.Services.AddScoped<IClienteService, ClienteService>();
builder.Services.AddScoped<IActivoService, ActivoService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<IReporteService, ReporteService>();

// ----------------------------------------------------------------------------
// 6. Manejo global de excepciones -> respuestas ProblemDetails
// ----------------------------------------------------------------------------
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// ----------------------------------------------------------------------------
// 7. Autenticación JWT + autorización basada en roles (Admin/Tecnico/Lector).
//    La clave de firma NO vive en appsettings.json: en Development sale de
//    user-secrets (dotnet user-secrets set "Jwt:Key" "..."); en un despliegue real
//    debería salir de una variable de entorno o un gestor de secretos.
// ----------------------------------------------------------------------------
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException(
        "Falta Jwt:Key. En desarrollo: dotnet user-secrets set \"Jwt:Key\" \"<valor>\" dentro de backend/Nucleo.Api.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// El manejador de excepciones va primero para envolver todo el pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Solo en desarrollo: aplica migraciones pendientes y siembra datos de prueba al arrancar.
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    await DbSeeder.SeedAsync(db);
}

app.UseHttpsRedirection();

// UseAuthentication ANTES de UseAuthorization: primero identifica quién es (valida el JWT
// y puebla User.Claims), luego decide qué puede hacer ([Authorize]/[Authorize(Roles=...)]).
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
