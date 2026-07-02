using Microsoft.EntityFrameworkCore;
using Nucleo.Api.Common;
using Nucleo.Api.Data;
using Nucleo.Api.Repositories;
using Nucleo.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// ----------------------------------------------------------------------------
// 1. MVC / Controllers
// ----------------------------------------------------------------------------
builder.Services.AddControllers();

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

// ----------------------------------------------------------------------------
// 5. Servicios de dominio
// ----------------------------------------------------------------------------
builder.Services.AddScoped<IClienteService, ClienteService>();

// ----------------------------------------------------------------------------
// 6. Manejo global de excepciones -> respuestas ProblemDetails
// ----------------------------------------------------------------------------
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

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
app.UseAuthorization();
app.MapControllers();

app.Run();
