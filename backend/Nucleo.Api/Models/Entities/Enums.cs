namespace Nucleo.Api.Models.Entities;

/// <summary>Tipo de activo administrado para un cliente.</summary>
public enum TipoActivo
{
    Hardware,
    Software,
    EquipoRed
}

/// <summary>Estado operativo de un activo. Su cambio se audita en <see cref="HistorialActivo"/>.</summary>
public enum EstadoActivo
{
    Operativo,
    EnReparacion,
    EnAlmacen,
    Retirado
}

/// <summary>Prioridad de atención de un ticket de servicio.</summary>
public enum Prioridad
{
    Baja,
    Media,
    Alta,
    Critica
}

/// <summary>Ciclo de vida de un ticket de servicio.</summary>
public enum EstadoTicket
{
    Abierto,
    EnProgreso,
    Resuelto,
    Cerrado
}

/// <summary>Rol del técnico dentro del sistema (se usará para autorización con JWT en una fase posterior).</summary>
public enum RolTecnico
{
    Tecnico,
    Supervisor,
    Administrador
}
