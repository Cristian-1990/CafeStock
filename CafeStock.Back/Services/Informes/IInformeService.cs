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

    // ---- Resumen ----

    /// <summary>Gasto de cada uno de los últimos <paramref name="meses"/> meses naturales (con los meses sin gasto a 0).</summary>
    List<PuntoDato> CalcularEvolucionGastoMensual(IEnumerable<Compra> compras, int meses = 12);

    /// <summary>Valor del inventario: Σ stock actual × precio de referencia (la misma fórmula que el snapshot diario).</summary>
    decimal CalcularValorInventario(IEnumerable<Producto> productos);

    /// <summary>Productos con máximo definido y sin existencias.</summary>
    List<Producto> CalcularProductosAgotados(IEnumerable<Producto> productos);

    /// <summary>
    /// Productos cuyo precio de la última compra difiere del de la compra anterior, de mayor a menor
    /// variación absoluta. Solo cuentan los productos con al menos dos compras.
    /// </summary>
    List<VariacionPrecio> CalcularVariacionesPrecio(IEnumerable<Compra> compras);

    ResumenInforme CalcularResumen(
        IEnumerable<Compra> compras, IEnumerable<Presupuesto> presupuestos,
        IEnumerable<Proveedor> proveedores, IEnumerable<Producto> productos);

    /// <summary>Avisos accionables, de más a menos graves, como mucho <paramref name="maximo"/>.</summary>
    List<AvisoInforme> CalcularAvisos(
        IEnumerable<Compra> compras, IEnumerable<Presupuesto> presupuestos,
        IEnumerable<Proveedor> proveedores, IEnumerable<Producto> productos, int maximo = 5);
}
