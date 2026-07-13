using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Nucleo.Api.Common.Exceptions;
using Nucleo.Api.Domain;
using Nucleo.Api.Models.DTOs;
using Nucleo.Api.Models.Entities;
using Nucleo.Api.Repositories;

namespace Nucleo.Api.Services;

/// <summary>
/// Reglas de negocio de Activo. Orquesta IActivoRepositorio, IHistorialActivoRepositorio
/// e IRepositorio&lt;Cliente&gt; (genérico, solo para validar existencia del cliente dueño).
/// CambiarEstadoAsync es el método clave: usa IUnitOfWork para envolver el cambio de
/// estado + el registro de auditoría en una sola transacción explícita.
/// </summary>
public class ActivoService : IActivoService
{
    private readonly IActivoRepositorio _activoRepositorio;
    private readonly IHistorialActivoRepositorio _historialRepositorio;
    private readonly IRepositorio<Cliente> _clienteRepositorio;
    private readonly IUnitOfWork _unitOfWork;

    public ActivoService(
        IActivoRepositorio activoRepositorio,
        IHistorialActivoRepositorio historialRepositorio,
        IRepositorio<Cliente> clienteRepositorio,
        IUnitOfWork unitOfWork)
    {
        _activoRepositorio = activoRepositorio;
        _historialRepositorio = historialRepositorio;
        _clienteRepositorio = clienteRepositorio;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<ActivoResponseDto>> ObtenerTodosAsync(int? clienteId, CancellationToken ct = default)
    {
        var activos = await _activoRepositorio.ObtenerTodosConClienteAsync(clienteId, ct);
        // Lambda explícita (no method group): MapearAResponse tiene un parámetro opcional,
        // lo que vuelve ambiguo para el compilador elegir entre los overloads de Select.
        return activos.Select(a => MapearAResponse(a)).ToList();
    }

    public async Task<ActivoResponseDto?> ObtenerPorIdAsync(int id, CancellationToken ct = default)
    {
        var activo = await _activoRepositorio.ObtenerPorIdConClienteAsync(id, ct);
        return activo is null ? null : MapearAResponse(activo);
    }

    public async Task<ActivoResponseDto> CrearAsync(CrearActivoDto dto, CancellationToken ct = default)
    {
        var cliente = await _clienteRepositorio.ObtenerPorIdAsync(dto.ClienteId, ct)
            ?? throw new RecursoNoEncontradoException("cliente", dto.ClienteId);

        var numeroSerie = dto.NumeroSerie.Trim();
        if (await _activoRepositorio.ExisteNumeroSerieAsync(numeroSerie, null, ct))
            throw new ConflictoException($"Ya existe un activo con el número de serie '{numeroSerie}'.");

        var activo = new Activo
        {
            ClienteId = dto.ClienteId,
            Tipo = dto.Tipo,
            Nombre = dto.Nombre.Trim(),
            NumeroSerie = numeroSerie,
            Estado = EstadoActivo.Operativo,
            FechaAdquisicion = dto.FechaAdquisicion
        };

        await _activoRepositorio.AgregarAsync(activo, ct);
        await _activoRepositorio.GuardarCambiosAsync(ct);

        return MapearAResponse(activo, cliente.Nombre);
    }

    public async Task ActualizarAsync(int id, ActualizarActivoDto dto, CancellationToken ct = default)
    {
        var activo = await _activoRepositorio.ObtenerPorIdAsync(id, ct)
            ?? throw new RecursoNoEncontradoException("activo", id);

        var numeroSerie = dto.NumeroSerie.Trim();
        if (await _activoRepositorio.ExisteNumeroSerieAsync(numeroSerie, id, ct))
            throw new ConflictoException($"Ya existe otro activo con el número de serie '{numeroSerie}'.");

        activo.Tipo = dto.Tipo;
        activo.Nombre = dto.Nombre.Trim();
        activo.NumeroSerie = numeroSerie;
        activo.FechaAdquisicion = dto.FechaAdquisicion;

        _activoRepositorio.Actualizar(activo);
        await _activoRepositorio.GuardarCambiosAsync(ct);
    }

    public async Task EliminarAsync(int id, CancellationToken ct = default)
    {
        var activo = await _activoRepositorio.ObtenerPorIdAsync(id, ct)
            ?? throw new RecursoNoEncontradoException("activo", id);

        _activoRepositorio.Eliminar(activo);
        await _activoRepositorio.GuardarCambiosAsync(ct);
    }

    public async Task<ActivoResponseDto> CambiarEstadoAsync(int id, CambiarEstadoActivoDto dto, int tecnicoId, CancellationToken ct = default)
    {
        var activo = await _activoRepositorio.ObtenerPorIdAsync(id, ct)
            ?? throw new RecursoNoEncontradoException("activo", id);

        if (!EstadoActivoTransiciones.EsValida(activo.Estado, dto.NuevoEstado))
        {
            var permitidas = EstadoActivoTransiciones.TransicionesDesde(activo.Estado);
            throw new ConflictoException(
                $"No se puede cambiar el activo de '{activo.Estado}' a '{dto.NuevoEstado}'. " +
                $"Transiciones permitidas desde '{activo.Estado}': " +
                $"{(permitidas.Count == 0 ? "ninguna (estado terminal)" : string.Join(", ", permitidas))}.");
        }

        var estadoAnterior = activo.Estado;

        // --- Transacción explícita: el cambio de estado y el registro de auditoría ---
        // --- viven en dos SaveChanges distintos (uno por repo); deben ser atómicos. ---
        await _unitOfWork.IniciarTransaccionAsync(ct);
        try
        {
            // 1) Cambio de estado del activo.
            activo.Estado = dto.NuevoEstado;
            _activoRepositorio.Actualizar(activo);
            await _activoRepositorio.GuardarCambiosAsync(ct);

            // 2) Registro de auditoría. Si TecnicoId no existe, el FK de SQL Server
            //    rechaza este INSERT (DbUpdateException) y saltamos al catch de abajo.
            var historial = new HistorialActivo
            {
                ActivoId = id,
                EstadoAnterior = estadoAnterior,
                EstadoNuevo = dto.NuevoEstado,
                Motivo = dto.Motivo.Trim(),
                Fecha = DateTime.UtcNow,
                TecnicoId = tecnicoId
            };
            await _historialRepositorio.AgregarAsync(historial, ct);
            await _historialRepositorio.GuardarCambiosAsync(ct);

            await _unitOfWork.ConfirmarTransaccionAsync(ct);
        }
        catch (DbUpdateException ex) when (EsViolacionForeignKey(ex))
        {
            await _unitOfWork.RevertirTransaccionAsync(ct);
            throw new RecursoNoEncontradoException("técnico", tecnicoId);
        }
        catch
        {
            await _unitOfWork.RevertirTransaccionAsync(ct);
            throw;
        }

        var actualizado = await _activoRepositorio.ObtenerPorIdConClienteAsync(id, ct);
        return MapearAResponse(actualizado!);
    }

    public async Task<IReadOnlyList<HistorialActivoResponseDto>> ObtenerHistorialAsync(int activoId, CancellationToken ct = default)
    {
        if (!await _activoRepositorio.ExisteAsync(activoId, ct))
            throw new RecursoNoEncontradoException("activo", activoId);

        var historial = await _historialRepositorio.ObtenerPorActivoIdAsync(activoId, ct);
        return historial.Select(MapearHistorialAResponse).ToList();
    }

    /// <summary>Error 547 = violación de FOREIGN KEY / CHECK constraint en SQL Server.</summary>
    private static bool EsViolacionForeignKey(DbUpdateException ex)
        => ex.InnerException is SqlException sql && sql.Number == 547;

    private static ActivoResponseDto MapearAResponse(Activo a, string? clienteNombreFallback = null) => new()
    {
        Id = a.Id,
        ClienteId = a.ClienteId,
        ClienteNombre = a.Cliente?.Nombre ?? clienteNombreFallback ?? string.Empty,
        Tipo = a.Tipo,
        Nombre = a.Nombre,
        NumeroSerie = a.NumeroSerie,
        Estado = a.Estado,
        FechaAdquisicion = a.FechaAdquisicion
    };

    private static HistorialActivoResponseDto MapearHistorialAResponse(HistorialActivo h) => new()
    {
        Id = h.Id,
        ActivoId = h.ActivoId,
        EstadoAnterior = h.EstadoAnterior,
        EstadoNuevo = h.EstadoNuevo,
        Motivo = h.Motivo,
        Fecha = h.Fecha,
        TecnicoId = h.TecnicoId,
        TecnicoNombre = h.Tecnico?.Nombre ?? string.Empty
    };
}
