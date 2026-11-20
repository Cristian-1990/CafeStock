using CafeStock.Back.Models;
using CafeStock.Back.Models.Informes;

namespace CafeStock.Back.Services.Informes;

/// <summary>
/// Cálculos de los informes de la página Informes. Es puro: recibe los datos ya cargados y
/// devuelve resultados, sin acceder a la base de datos, de modo que se puede probar sin ella.
/// </summary>
public interface IInformeService
{
    /// <summary>Días sin compra a partir de los cuales un proveedor con reparto definido aparece como alerta.</summary>
    int UmbralDiasSinCompra { get; }

    GastoMensual CalcularGastoMesActual(IEnumerable<Compra> compras);

    IEnumerable<Compra> FiltrarCompras(
        IEnumerable<Compra> compras, RangoFechas rango, DateTime? fechaDesde, DateTime? fechaHasta, int? proveedorId);

    bool HayRangoLibre(DateTime? fechaDesde, DateTime? fechaHasta);

    List<PuntoDato> CalcularGastoPorProveedor(IEnumerable<Compra> compras);
    List<PuntoDato> CalcularEvolucionPrecio(IEnumerable<Compra> compras, int? productoId);
    List<PuntoDato> CalcularProductosPorGastoTotal(IEnumerable<Compra> compras);
    List<PuntoDato> CalcularGastoPorMetodoPago(IEnumerable<Compra> compras);
    List<ProveedorAlerta> CalcularProveedoresSinCompraReciente(IEnumerable<Compra> compras, IEnumerable<Proveedor> proveedores);
    List<PuntoDatoPpv> CalcularPpvPorProducto(IEnumerable<Compra> compras, IEnumerable<Producto> productos);
    List<PuntoDato> CalcularGastoPorTipoProveedor(IEnumerable<Compra> compras, IEnumerable<Proveedor> proveedores);
    List<PuntoDato> CalcularConcentracionGastoPorProveedor(IReadOnlyCollection<PuntoDato> gastoPorProveedorEuros);
    List<PuntoDato> CalcularTicketMedioPorProveedor(IEnumerable<Compra> compras);
    List<LineaExcedenteStock> CalcularRecepcionesConExcedente(IEnumerable<Compra> compras);
    List<InformeReparto> CalcularFrecuenciaRepartoVsEsperado(IEnumerable<Compra> compras, IEnumerable<Proveedor> proveedores);
    PresupuestoVsRealResultado CalcularPresupuestoVsReal(
        IEnumerable<Compra> compras, IEnumerable<Presupuesto> presupuestos, int mes, int anio);
}
