using Nucleo.Api.Models.Entities;

namespace Nucleo.Api.Domain;

/// <summary>
/// Máquina de estados de un Ticket: Abierto → EnProgreso → Resuelto → Cerrado,
/// con una salida de escape Abierto → Cancelado. Cerrado y Cancelado son terminales.
/// A diferencia de Activo, un Ticket no tiene tabla de auditoría (no existe
/// "HistorialTicket"), así que el service que consulta esta tabla no necesita
/// envolver el cambio en una transacción explícita: es un único UPDATE.
/// </summary>
public static class EstadoTicketTransiciones
{
    private static readonly Dictionary<EstadoTicket, EstadoTicket[]> Permitidas = new()
    {
        [EstadoTicket.Abierto] = [EstadoTicket.EnProgreso, EstadoTicket.Cancelado],
        [EstadoTicket.EnProgreso] = [EstadoTicket.Resuelto],
        [EstadoTicket.Resuelto] = [EstadoTicket.Cerrado],
        [EstadoTicket.Cerrado] = [],
        [EstadoTicket.Cancelado] = []
    };

    /// <summary>Estados en los que se considera que el ticket sigue "abierto" para efectos de reportes.</summary>
    public static readonly EstadoTicket[] EstadosAbiertos = [EstadoTicket.Abierto, EstadoTicket.EnProgreso];

    /// <summary>Estados terminales: al entrar aquí se registra FechaCierre.</summary>
    public static readonly EstadoTicket[] EstadosTerminales = [EstadoTicket.Cerrado, EstadoTicket.Cancelado];

    public static bool EsValida(EstadoTicket actual, EstadoTicket nuevo)
        => actual != nuevo && Permitidas[actual].Contains(nuevo);

    public static IReadOnlyList<EstadoTicket> TransicionesDesde(EstadoTicket actual)
        => Permitidas[actual];
}
