using Moq;
using Nucleo.Api.Common.Exceptions;
using Nucleo.Api.Models.DTOs;
using Nucleo.Api.Models.Entities;
using Nucleo.Api.Repositories;
using Nucleo.Api.Services;

namespace Nucleo.Api.Tests.Services;

/// <summary>
/// Pruebas de AuthService con BCrypt REAL (no mockeado: verificar el hash es parte de la
/// lógica bajo prueba) y el generador de tokens mockeado.
/// </summary>
public class AuthServiceTests
{
    private const string PasswordCorrecta = "Nucleo123!";

    private readonly Mock<ITecnicoRepositorio> _tecnicoRepo = new();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        _service = new AuthService(_tecnicoRepo.Object, _tokenService.Object);
    }

    private static Tecnico TecnicoDemo() => new()
    {
        Id = 4,
        Nombre = "Carlos Méndez",
        Email = "carlos.mendez@nucleo.mx",
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(PasswordCorrecta),
        Rol = RolTecnico.Tecnico
    };

    [Fact]
    public async Task LoginAsync_CredencialesValidas_DevuelveTokenYDatosDelTecnico()
    {
        var tecnico = TecnicoDemo();
        var expira = DateTime.UtcNow.AddHours(2);
        _tecnicoRepo.Setup(r => r.ObtenerPorEmailAsync("carlos.mendez@nucleo.mx", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(tecnico);
        _tokenService.Setup(t => t.GenerarToken(tecnico)).Returns(("jwt-de-prueba", expira));

        var resultado = await _service.LoginAsync(new LoginDto { Email = "carlos.mendez@nucleo.mx", Password = PasswordCorrecta });

        Assert.Equal("jwt-de-prueba", resultado.Token);
        Assert.Equal(expira, resultado.ExpiraEn);
        Assert.Equal(4, resultado.TecnicoId);
        Assert.Equal(RolTecnico.Tecnico, resultado.Rol);
    }

    [Fact]
    public async Task LoginAsync_EmailInexistente_LanzaCredencialesInvalidas()
    {
        _tecnicoRepo.Setup(r => r.ObtenerPorEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((Tecnico?)null);

        await Assert.ThrowsAsync<CredencialesInvalidasException>(
            () => _service.LoginAsync(new LoginDto { Email = "nadie@nucleo.mx", Password = "loquesea" }));

        // Nunca se genera token si el email no existe.
        _tokenService.Verify(t => t.GenerarToken(It.IsAny<Tecnico>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_PasswordIncorrecta_LanzaLaMismaExcepcionQueEmailInexistente()
    {
        _tecnicoRepo.Setup(r => r.ObtenerPorEmailAsync("carlos.mendez@nucleo.mx", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(TecnicoDemo());

        // Mismo tipo de excepción (y por tanto mismo mensaje HTTP) que cuando el email
        // no existe: no filtrar cuál de las dos credenciales falló.
        await Assert.ThrowsAsync<CredencialesInvalidasException>(
            () => _service.LoginAsync(new LoginDto { Email = "carlos.mendez@nucleo.mx", Password = "incorrecta" }));

        _tokenService.Verify(t => t.GenerarToken(It.IsAny<Tecnico>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_RecortaEspaciosDelEmail()
    {
        var tecnico = TecnicoDemo();
        _tecnicoRepo.Setup(r => r.ObtenerPorEmailAsync("carlos.mendez@nucleo.mx", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(tecnico);
        _tokenService.Setup(t => t.GenerarToken(tecnico)).Returns(("jwt", DateTime.UtcNow));

        var resultado = await _service.LoginAsync(new LoginDto { Email = "  carlos.mendez@nucleo.mx  ", Password = PasswordCorrecta });

        Assert.Equal("jwt", resultado.Token);
        _tecnicoRepo.Verify(r => r.ObtenerPorEmailAsync("carlos.mendez@nucleo.mx", It.IsAny<CancellationToken>()), Times.Once);
    }
}
