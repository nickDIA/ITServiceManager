using Nucleo.Api.Common.Exceptions;
using Nucleo.Api.Models.DTOs;
using Nucleo.Api.Models.Entities;
using Nucleo.Api.Repositories;

namespace Nucleo.Api.Services;

/// <summary>
/// Reglas de negocio de Cliente. Orquesta el repositorio, aplica validaciones de dominio
/// (RFC único, integridad referencial al borrar) y mapea entre entidad y DTO.
/// </summary>
public class ClienteService : IClienteService
{
    private readonly IClienteRepositorio _repositorio;

    public ClienteService(IClienteRepositorio repositorio)
    {
        _repositorio = repositorio;
    }

    public async Task<IReadOnlyList<ClienteResponseDto>> ObtenerTodosAsync(CancellationToken ct = default)
    {
        var clientes = await _repositorio.ObtenerTodosAsync(ct);
        return clientes.Select(MapearAResponse).ToList();
    }

    public async Task<ClienteResponseDto?> ObtenerPorIdAsync(int id, CancellationToken ct = default)
    {
        var cliente = await _repositorio.ObtenerPorIdAsync(id, ct);
        return cliente is null ? null : MapearAResponse(cliente);
    }

    public async Task<ClienteResponseDto> CrearAsync(CrearClienteDto dto, CancellationToken ct = default)
    {
        var rfc = NormalizarRfc(dto.Rfc);

        if (await _repositorio.ExisteRfcAsync(rfc, null, ct))
            throw new ConflictoException($"Ya existe un cliente con el RFC '{rfc}'.");

        var cliente = new Cliente
        {
            Nombre = dto.Nombre.Trim(),
            Rfc = rfc,
            Contacto = dto.Contacto?.Trim(),
            Telefono = dto.Telefono?.Trim(),
            Activo = true
        };

        await _repositorio.AgregarAsync(cliente, ct);
        await _repositorio.GuardarCambiosAsync(ct);

        return MapearAResponse(cliente);
    }

    public async Task ActualizarAsync(int id, ActualizarClienteDto dto, CancellationToken ct = default)
    {
        var cliente = await _repositorio.ObtenerPorIdAsync(id, ct);
        if (cliente is null)
            throw new RecursoNoEncontradoException("cliente", id);

        var rfc = NormalizarRfc(dto.Rfc);

        // El RFC puede repetirse solo si es el mismo registro.
        if (await _repositorio.ExisteRfcAsync(rfc, id, ct))
            throw new ConflictoException($"Ya existe otro cliente con el RFC '{rfc}'.");

        cliente.Nombre = dto.Nombre.Trim();
        cliente.Rfc = rfc;
        cliente.Contacto = dto.Contacto?.Trim();
        cliente.Telefono = dto.Telefono?.Trim();
        cliente.Activo = dto.Activo;

        _repositorio.Actualizar(cliente);
        await _repositorio.GuardarCambiosAsync(ct);
    }

    public async Task EliminarAsync(int id, CancellationToken ct = default)
    {
        var cliente = await _repositorio.ObtenerPorIdAsync(id, ct);
        if (cliente is null)
            throw new RecursoNoEncontradoException("cliente", id);

        if (await _repositorio.TieneActivosAsync(id, ct))
            throw new ConflictoException(
                "No se puede eliminar el cliente porque tiene activos asociados. " +
                "Reasigna o elimina sus activos primero, o desactívalo (baja lógica) en su lugar.");

        _repositorio.Eliminar(cliente);
        await _repositorio.GuardarCambiosAsync(ct);
    }

    private static string NormalizarRfc(string rfc) => rfc.Trim().ToUpperInvariant();

    private static ClienteResponseDto MapearAResponse(Cliente c) => new()
    {
        Id = c.Id,
        Nombre = c.Nombre,
        Rfc = c.Rfc,
        Contacto = c.Contacto,
        Telefono = c.Telefono,
        Activo = c.Activo
    };
}
