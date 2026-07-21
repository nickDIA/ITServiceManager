namespace Nucleo.Api.Models.DTOs;

/// <summary>
/// Envoltura genérica para listados paginados. HayMas evita que el cliente tenga que
/// calcular pagina*tamanoPagina contra el total para saber si pedir la siguiente página.
/// </summary>
public class ResultadoPaginadoDto<T>
{
    public IReadOnlyList<T> Items { get; set; } = [];
    public int Pagina { get; set; }
    public int TamanoPagina { get; set; }
    public int TotalRegistros { get; set; }
    public bool HayMas => (long)Pagina * TamanoPagina < TotalRegistros;
}
