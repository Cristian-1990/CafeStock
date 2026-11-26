using CafeStock.Back.Models;

namespace CafeStock.Back.Entity;

public class ProductoEntity
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public decimal StockActual { get; set; }
    public decimal StockMaximo { get; set; }
    public UnidadMedida Unidad { get; set; } = UnidadMedida.SinEspecificar;
    public string Descripcion { get; set; } = string.Empty;
    public string ImagenUrl { get; set; } = string.Empty;
    public int? ProveedorId { get; set; }
    public ProveedorEntity? Proveedor { get; set; }
    public decimal PrecioUnitario { get; set; }
    public bool SeguimientoIndividual { get; set; }
    public GrupoCafe GrupoCafe { get; set; }
    public FormatoCafe FormatoCafe { get; set; }
    public bool AplicaConciliacionTpv { get; set; }
    public int? UnidadesPorPack { get; set; }
    public int? Pasillo { get; set; }
    public int OrdenEnPasillo { get; set; }
    public bool EsFrontalPasillo { get; set; }
    public int? Armario { get; set; }
    public SeccionEspecial Seccion { get; set; }
    public string Pais { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public string Finca { get; set; } = string.Empty;
    public string Altitud { get; set; } = string.Empty;
    public string Variedad { get; set; } = string.Empty;
    public string Proceso { get; set; } = string.Empty;
    public string NotasCata { get; set; } = string.Empty;
}