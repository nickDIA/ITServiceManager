using Nucleo.Api.Common.Exceptions;
using Nucleo.Api.Domain;
using Nucleo.Api.Models.DTOs;
using Nucleo.Api.Models.Entities;
using Nucleo.Api.Repositories;

namespace Nucleo.Api.Services;

public class TicketService : ITicketService
{
    private readonly ITicketRepositorio _ticketRepositorio;
    private readonly IRepositorio<Cliente> _clienteRepositorio;
    private readonly IRepositorio<Activo> _activoRepositorio;
    private readonly IRepositorio<Tecnico> _tecnicoRepositorio;

    public TicketService(
        ITicketRepositorio ticketRepositorio,
        IRepositorio<Cliente> clienteRepositorio,
        IRepositorio<Activo> activoRepositorio,
        IRepositorio<Tecnico> tecnicoRepositorio)
    {
        _ticketRepositorio = ticketRepositorio;
        _clienteRepositorio = clienteRepositorio;
        _activoRepositorio = activoRepositorio;
        _tecnicoRepositorio = tecnicoRepositorio;
    }

    public async Task<ResultadoPaginadoDto<TicketResponseDto>> ObtenerTodosAsync(
        int? clienteId, int? tecnicoId, EstadoTicket? estado, int pagina, int tamano, CancellationToken ct = default)
    {
        pagina = Math.Max(1, pagina);
        tamano = Math.Clamp(tamano, 1, 100);

        var (items, total) = await _ticketRepositorio.ObtenerPaginadoConJoinsAsync(
            pagina, tamano, clienteId, tecnicoId, estado, ct);

        return new ResultadoPaginadoDto<TicketResponseDto>
        {
            Items = items.Select(MapearAResponse).ToList(),
            Pagina = pagina,
            TamanoPagina = tamano,
            TotalRegistros = total
        };
    }

    public async Task<TicketResponseDto?> ObtenerPorIdAsync(int id, CancellationToken ct = default)
    {
        var ticket = await _ticketRepositorio.ObtenerPorIdConJoinsAsync(id, ct);
        return ticket is null ? null : MapearAResponse(ticket);
    }

    public async Task<TicketResponseDto> CrearAsync(CrearTicketDto dto, CancellationToken ct = default)
    {
        if (!await _clienteRepositorio.ExisteAsync(dto.ClienteId, ct))
            throw new RecursoNoEncontradoException("cliente", dto.ClienteId);

        if (!await _tecnicoRepositorio.ExisteAsync(dto.TecnicoId, ct))
            throw new RecursoNoEncontradoException("técnico", dto.TecnicoId);

        await ValidarActivoDelClienteAsync(dto.ActivoId, dto.ClienteId, ct);

        var ticket = new Ticket
        {
            ClienteId = dto.ClienteId,
            ActivoId = dto.ActivoId,
            Titulo = dto.Titulo.Trim(),
            Descripcion = dto.Descripcion.Trim(),
            Prioridad = dto.Prioridad,
            Estado = EstadoTicket.Abierto,
            TecnicoId = dto.TecnicoId,
            FechaCreacion = DateTime.UtcNow
        };

        await _ticketRepositorio.AgregarAsync(ticket, ct);
        await _ticketRepositorio.GuardarCambiosAsync(ct);

        var creado = await _ticketRepositorio.ObtenerPorIdConJoinsAsync(ticket.Id, ct);
        return MapearAResponse(creado!);
    }

    public async Task ActualizarAsync(int id, ActualizarTicketDto dto, CancellationToken ct = default)
    {
        var ticket = await _ticketRepositorio.ObtenerPorIdAsync(id, ct)
            ?? throw new RecursoNoEncontradoException("ticket", id);

        if (!await _tecnicoRepositorio.ExisteAsync(dto.TecnicoId, ct))
            throw new RecursoNoEncontradoException("técnico", dto.TecnicoId);

        await ValidarActivoDelClienteAsync(dto.ActivoId, ticket.ClienteId, ct);

        ticket.ActivoId = dto.ActivoId;
        ticket.Titulo = dto.Titulo.Trim();
        ticket.Descripcion = dto.Descripcion.Trim();
        ticket.Prioridad = dto.Prioridad;
        ticket.TecnicoId = dto.TecnicoId;

        _ticketRepositorio.Actualizar(ticket);
        await _ticketRepositorio.GuardarCambiosAsync(ct);
    }

    public async Task EliminarAsync(int id, CancellationToken ct = default)
    {
        var ticket = await _ticketRepositorio.ObtenerPorIdAsync(id, ct)
            ?? throw new RecursoNoEncontradoException("ticket", id);

        _ticketRepositorio.Eliminar(ticket);
        await _ticketRepositorio.GuardarCambiosAsync(ct);
    }

    public async Task<TicketResponseDto> CambiarEstadoAsync(int id, CambiarEstadoTicketDto dto, CancellationToken ct = default)
    {
        var ticket = await _ticketRepositorio.ObtenerPorIdAsync(id, ct)
            ?? throw new RecursoNoEncontradoException("ticket", id);

        if (!EstadoTicketTransiciones.EsValida(ticket.Estado, dto.NuevoEstado))
        {
            var permitidas = EstadoTicketTransiciones.TransicionesDesde(ticket.Estado);
            throw new ConflictoException(
                $"No se puede cambiar el ticket de '{ticket.Estado}' a '{dto.NuevoEstado}'. " +
                $"Transiciones permitidas desde '{ticket.Estado}': " +
                $"{(permitidas.Count == 0 ? "ninguna (estado terminal)" : string.Join(", ", permitidas))}.");
        }

        ticket.Estado = dto.NuevoEstado;

        // Solo un UPDATE: sin tabla de auditoría para Ticket, no hace falta IUnitOfWork aquí
        // (contraste con ActivoService.CambiarEstadoAsync, que sí toca dos repos/tablas).
        if (EstadoTicketTransiciones.EstadosTerminales.Contains(dto.NuevoEstado))
            ticket.FechaCierre = DateTime.UtcNow;

        _ticketRepositorio.Actualizar(ticket);
        await _ticketRepositorio.GuardarCambiosAsync(ct);

        var actualizado = await _ticketRepositorio.ObtenerPorIdConJoinsAsync(id, ct);
        return MapearAResponse(actualizado!);
    }

    /// <summary>Si se especifica un activo, debe existir y pertenecer al mismo cliente del ticket.</summary>
    private async Task ValidarActivoDelClienteAsync(int? activoId, int clienteId, CancellationToken ct)
    {
        if (activoId is null)
            return;

        var activo = await _activoRepositorio.ObtenerPorIdAsync(activoId.Value, ct)
            ?? throw new RecursoNoEncontradoException("activo", activoId.Value);

        if (activo.ClienteId != clienteId)
            throw new ConflictoException(
                $"El activo '{activoId}' no pertenece al cliente '{clienteId}'.");
    }

    private static TicketResponseDto MapearAResponse(Ticket t) => new()
    {
        Id = t.Id,
        ClienteId = t.ClienteId,
        ClienteNombre = t.Cliente?.Nombre ?? string.Empty,
        ActivoId = t.ActivoId,
        ActivoNombre = t.Activo?.Nombre,
        Titulo = t.Titulo,
        Descripcion = t.Descripcion,
        Prioridad = t.Prioridad,
        Estado = t.Estado,
        TecnicoId = t.TecnicoId,
        TecnicoNombre = t.Tecnico?.Nombre ?? string.Empty,
        FechaCreacion = t.FechaCreacion,
        FechaCierre = t.FechaCierre,
        SlaHoras = ObtenerSlaHoras(t.Cliente)
    };

    /// <summary>
    /// SLA del contrato activo del cliente. Si por alguna razón hay más de uno activo a la
    /// vez (no hay restricción de unicidad en BD), se toma el más estricto (mínimo) en vez
    /// de uno arbitrario — más seguro para una alerta que para un promedio.
    /// </summary>
    private static int? ObtenerSlaHoras(Cliente? cliente)
    {
        var slasActivos = cliente?.Contratos.Where(c => c.Activo).Select(c => c.SlaHoras).ToList();
        return slasActivos is { Count: > 0 } ? slasActivos.Min() : null;
    }
}
