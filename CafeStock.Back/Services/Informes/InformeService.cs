using System.Globalization;
using CafeStock.Back.Models;
using CafeStock.Back.Models.Informes;

namespace CafeStock.Back.Services.Informes;

/// <summary>
/// Lógica de cálculo de los informes, extraída de Informes.razor sin cambiar su comportamiento
/// para poder probarla. La única fuente de "hoy" es el <see cref="TimeProvider"/> inyectado (en
/// producción, el reloj del sistema), lo que permite fijar la fecha en los tests.
/// </summary>
public class InformeService : IInformeService
{
    public const int UmbralDias = 14;
    private const decimal UmbralPorcentajeCumplimientoReparto = 50m;

    // Proveedor.DiaReparto es texto libre ("Miércoles 9:00", "Martes, Miércoles y Jueves"),
    // no un día limpio. Por eso la comparación es "¿el nombre del día real está contenido en el
    // texto?" en vez de una igualdad estricta contra un único DayOfWeek.
    private static readonly string[] NombresDiasSemana =
        ["Domingo", "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado"];

    private readonly TimeProvider _reloj;

    public InformeService(TimeProvider reloj)
    {
        _reloj = reloj;
    }

    public int UmbralDiasSinCompra => UmbralDias;

    private DateTime Ahora => _reloj.GetLocalNow().DateTime;

    /// <summary>
    /// Importe total de una compra: la suma de sus líneas (cantidad × precio real) MÁS el importe de
    /// las compras excepcionales (artículos sueltos del supermercado, que no tienen línea). Todos los
    /// informes de gasto pasan por aquí; antes solo sumaban las líneas y el gasto salía corto.
    /// </summary>
    public static decimal ImporteDe(Compra compra) =>
        compra.Lineas.Sum(l => l.Cantidad * l.PrecioUnitario) + compra.ImporteComprasExcepcionales;

    // ---- 1. Gasto del mes actual ----

    public GastoMensual CalcularGastoMesActual(IEnumerable<Compra> compras)
    {
        var lista = compras.ToList();
        var ahora = Ahora;
        var inicioMesActual = new DateTime(ahora.Year, ahora.Month, 1);
        var inicioMesAnterior = inicioMesActual.AddMonths(-1);

        var gastoMesActual = lista
            .Where(c => c.Fecha >= inicioMesActual && c.Fecha < inicioMesActual.AddMonths(1))
            .Sum(ImporteDe);

        var gastoMesAnterior = lista
            .Where(c => c.Fecha >= inicioMesAnterior && c.Fecha < inicioMesActual)
            .Sum(ImporteDe);

        decimal? variacion = gastoMesAnterior > 0
            ? (gastoMesActual - gastoMesAnterior) / gastoMesAnterior * 100
            : null;

        return new GastoMensual(gastoMesActual, variacion);
    }

    // ---- 2. Gasto por proveedor y filtros ----

    public List<PuntoDato> CalcularGastoPorProveedor(IEnumerable<Compra> compras) =>
        AgruparGastoPorProveedorConId(compras)
            .Select(g => new PuntoDato(g.Nombre, g.Gasto))
            .ToList();

    /// <summary>
    /// Núcleo de "Gasto por proveedor" (Σ Cantidad×PrecioUnitario, agrupado por proveedor),
    /// conservando el ProveedorId además del nombre: "Presupuesto vs Real" (12) lo reutiliza y
    /// necesita el Id para cruzar con Presupuesto.ProveedorId.
    /// </summary>
    private static List<(int? ProveedorId, string Nombre, decimal Gasto)> AgruparGastoPorProveedorConId(IEnumerable<Compra> compras) =>
        AgruparComprasPorProveedor(compras)
            .Select(g => (
                ProveedorId: g.Key,
                Nombre: ResolverNombreProveedorDeGrupo(g),
                Gasto: g.Sum(ImporteDe)))
            .OrderByDescending(t => t.Gasto)
            .ToList();

    private static IEnumerable<IGrouping<int?, Compra>> AgruparComprasPorProveedor(IEnumerable<Compra> compras) =>
        compras.GroupBy(c => c.ProveedorId);

    private static string ResolverNombreProveedorDeGrupo(IGrouping<int?, Compra> grupo) =>
        grupo.First().ProveedorNombre ?? "Sin proveedor asignado";

    /// <summary>Filtro combinado de rango de fechas + proveedor concreto (informes 2 y 10).</summary>
    public IEnumerable<Compra> FiltrarCompras(
        IEnumerable<Compra> compras, RangoFechas rango, DateTime? fechaDesde, DateTime? fechaHasta, int? proveedorId)
    {
        var resultado = FiltrarPorRango(compras, rango, fechaDesde, fechaHasta);
        return proveedorId is int id ? resultado.Where(c => c.ProveedorId == id) : resultado;
    }

    /// <summary>
    /// El rango libre (fechaDesde + fechaHasta, ambas puestas) tiene prioridad sobre el chip
    /// preestablecido. Con una sola fecha puesta (el usuario todavía eligiendo) se sigue
    /// aplicando el chip, para no filtrar con un rango a medias.
    /// </summary>
    private IEnumerable<Compra> FiltrarPorRango(IEnumerable<Compra> compras, RangoFechas rango, DateTime? fechaDesde, DateTime? fechaHasta)
    {
        if (HayRangoLibre(fechaDesde, fechaHasta))
            return compras.Where(c => c.Fecha.Date >= fechaDesde!.Value.Date && c.Fecha.Date <= fechaHasta!.Value.Date);

        var hoy = Ahora.Date;
        return rango switch
        {
            // Calendario real (nunca ventana móvil tipo "últimos N días/meses"): cada chip
            // arranca en una fecha de calendario fija y llega hasta hoy.
            RangoFechas.UltimaSemana => compras.Where(c => c.Fecha.Date >= LunesDeLaSemana(hoy) && c.Fecha.Date <= hoy),
            RangoFechas.UltimoMes => compras.Where(c => c.Fecha.Date >= new DateTime(hoy.Year, hoy.Month, 1) && c.Fecha.Date <= hoy),
            RangoFechas.UltimoAno => compras.Where(c => c.Fecha.Date >= new DateTime(hoy.Year, 1, 1) && c.Fecha.Date <= hoy),
            _ => compras
        };
    }

    /// <summary>Lunes de la semana ISO de <paramref name="fecha"/> (DayOfWeek.Sunday=0 en .NET, de ahí el +6 % 7).</summary>
    private static DateTime LunesDeLaSemana(DateTime fecha) => fecha.Date.AddDays(-(((int)fecha.DayOfWeek + 6) % 7));

    public bool HayRangoLibre(DateTime? fechaDesde, DateTime? fechaHasta) => fechaDesde is not null && fechaHasta is not null;

    // ---- 3. Evolución de precio por producto ----

    public List<PuntoDato> CalcularEvolucionPrecio(IEnumerable<Compra> compras, int? productoId)
    {
        if (productoId is null) return [];
        return compras
            .SelectMany(c => c.Lineas.Where(l => l.ProductoId == productoId).Select(l => new { c.Fecha, l.PrecioUnitario }))
            .OrderBy(x => x.Fecha)
            .Select(x => new PuntoDato(x.Fecha.ToString("dd/MM/yy"), x.PrecioUnitario))
            .ToList();
    }

    // ---- 4. Productos con más peso en el gasto ----

    public List<PuntoDato> CalcularProductosPorGastoTotal(IEnumerable<Compra> compras) =>
        compras
            .SelectMany(c => c.Lineas)
            .GroupBy(l => l.ProductoNombre ?? $"Producto #{l.ProductoId}")
            .Select(g => new PuntoDato(g.Key, g.Sum(l => l.Cantidad * l.PrecioUnitario)))
            .OrderByDescending(p => p.Valor)
            .Take(10)
            .ToList();

    // ---- 5. Gasto por método de pago ----

    public List<PuntoDato> CalcularGastoPorMetodoPago(IEnumerable<Compra> compras) =>
        compras
            .GroupBy(c => string.IsNullOrWhiteSpace(c.MetodoPago) ? "Sin especificar" : c.MetodoPago)
            .Select(g => new PuntoDato(g.Key, g.Sum(ImporteDe)))
            .OrderByDescending(p => p.Valor)
            .ToList();

    // ---- 6. Proveedores sin compra reciente ----

    public List<ProveedorAlerta> CalcularProveedoresSinCompraReciente(IEnumerable<Compra> compras, IEnumerable<Proveedor> proveedores)
    {
        var ultimaCompraPorProveedor = compras
            .Where(c => c.ProveedorId is not null)
            .GroupBy(c => c.ProveedorId!.Value)
            .ToDictionary(g => g.Key, g => g.Max(c => c.Fecha));

        var ahora = Ahora;
        return proveedores
            .Where(p => !string.IsNullOrWhiteSpace(p.DiaReparto))
            .Select(p => ultimaCompraPorProveedor.TryGetValue(p.Id, out var ultimaFecha)
                ? new ProveedorAlerta(p.Nombre, (ahora - ultimaFecha).Days)
                : new ProveedorAlerta(p.Nombre, null))
            .Where(a => a.DiasDesdeUltimaCompra is null or > UmbralDias)
            .OrderByDescending(a => a.DiasDesdeUltimaCompra ?? int.MaxValue)
            .ToList();
    }

    // ---- 7. PPV (Purchase Price Variance) por producto ----

    /// <summary>
    /// Para cada producto con al menos una LineaCompra y precio de referencia fijado
    /// (Producto.PrecioUnitario > 0): PPV = Σ (precio real de la línea − precio de referencia) ×
    /// Cantidad. PPV > 0 = se pagó de más; PPV &lt; 0 = de menos. El % compara el precio real
    /// medio ponderado por cantidad (no la media simple) contra la referencia.
    /// </summary>
    public List<PuntoDatoPpv> CalcularPpvPorProducto(IEnumerable<Compra> compras, IEnumerable<Producto> productos)
    {
        var productosPorId = productos.ToDictionary(p => p.Id);

        return compras
            .SelectMany(c => c.Lineas)
            .GroupBy(l => l.ProductoId)
            .Where(g => productosPorId.TryGetValue(g.Key, out var producto) && producto.PrecioUnitario > 0)
            .Select(g =>
            {
                var producto = productosPorId[g.Key];
                var ppv = g.Sum(l => (l.PrecioUnitario - producto.PrecioUnitario) * l.Cantidad);
                var cantidadTotal = g.Sum(l => l.Cantidad);
                var precioMedioPonderado = cantidadTotal > 0 ? g.Sum(l => l.PrecioUnitario * l.Cantidad) / cantidadTotal : 0;
                var porcentajeDesviacion = (precioMedioPonderado - producto.PrecioUnitario) / producto.PrecioUnitario * 100;
                return new PuntoDatoPpv(producto.Nombre, ppv, porcentajeDesviacion);
            })
            .OrderByDescending(p => Math.Abs(p.Valor))
            .ToList();
    }

    // ---- 8. Supermercado genérico vs proveedores habituales ----

    /// <summary>Las compras sin proveedor asignado quedan fuera: no se pueden clasificar sin proveedor.</summary>
    public List<PuntoDato> CalcularGastoPorTipoProveedor(IEnumerable<Compra> compras, IEnumerable<Proveedor> proveedores)
    {
        var proveedoresPorId = proveedores.ToDictionary(p => p.Id);

        return compras
            .Where(c => c.ProveedorId is not null && proveedoresPorId.ContainsKey(c.ProveedorId.Value))
            .GroupBy(c => proveedoresPorId[c.ProveedorId!.Value].EsSupermercadoGenerico)
            .Select(g => new PuntoDato(
                g.Key ? "Supermercado genérico" : "Proveedores habituales",
                g.Sum(ImporteDe)))
            .OrderByDescending(p => p.Valor)
            .ToList();
    }

    // ---- 9. Concentración de gasto por proveedor ----

    public List<PuntoDato> CalcularConcentracionGastoPorProveedor(IReadOnlyCollection<PuntoDato> gastoPorProveedorEuros)
    {
        var gastoTotal = gastoPorProveedorEuros.Sum(g => g.Valor);
        if (gastoTotal <= 0) return [];

        return gastoPorProveedorEuros
            .Select(g => new PuntoDato(g.Etiqueta, g.Valor / gastoTotal * 100))
            .OrderByDescending(p => p.Valor)
            .ToList();
    }

    // ---- 10. Ticket medio de compra por proveedor ----

    public List<PuntoDato> CalcularTicketMedioPorProveedor(IEnumerable<Compra> compras) =>
        AgruparComprasPorProveedor(compras)
            .Select(g => new PuntoDato(ResolverNombreProveedorDeGrupo(g), g.Average(ImporteDe)))
            .OrderByDescending(p => p.Valor)
            .ToList();

    // ---- 10b. Recepciones que superaron el stock máximo ----

    /// <summary>
    /// StockResultanteTrasRecepcion/StockMaximoEnMomento son nullable: las líneas anteriores a
    /// ese campo no tienen el dato y se excluyen. Solo cuenta como excedente cuando la resta
    /// es estrictamente positiva (0 = llegó justo al máximo, no lo superó).
    /// </summary>
    public List<LineaExcedenteStock> CalcularRecepcionesConExcedente(IEnumerable<Compra> compras) =>
        compras
            .SelectMany(c => c.Lineas.Select(l => (Compra: c, Linea: l)))
            .Where(x => x.Linea.StockResultanteTrasRecepcion is not null
                     && x.Linea.StockMaximoEnMomento is not null
                     && x.Linea.StockResultanteTrasRecepcion.Value - x.Linea.StockMaximoEnMomento.Value > 0)
            .Select(x => new LineaExcedenteStock(
                x.Compra.Fecha,
                x.Linea.ProductoNombre ?? $"Producto #{x.Linea.ProductoId}",
                x.Compra.ProveedorNombre ?? "Sin proveedor asignado",
                x.Linea.Cantidad,
                x.Linea.StockResultanteTrasRecepcion!.Value - x.Linea.StockMaximoEnMomento!.Value))
            .OrderByDescending(l => l.Fecha)
            .ToList();

    // ---- 11. Frecuencia real de compra vs día de reparto esperado ----

    /// <summary>
    /// OrdinalIgnoreCase ignora mayúsculas pero NO tildes: "MIERCOLES" tecleado a mano no haría
    /// match contra "Miércoles". CompareInfo con IgnoreNonSpace ignora además los diacríticos.
    /// </summary>
    private static bool DiaRepartoContiene(string diaReparto, string nombreDia) =>
        CultureInfo.InvariantCulture.CompareInfo.IndexOf(
            diaReparto, nombreDia, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0;

    /// <summary>
    /// Por cada proveedor con DiaReparto definido: de sus compras, cuántas cayeron en un día
    /// mencionado en DiaReparto. Se marca Desviacion cuando hay compras pero menos de la mitad
    /// cayeron en el día esperado.
    /// </summary>
    public List<InformeReparto> CalcularFrecuenciaRepartoVsEsperado(IEnumerable<Compra> compras, IEnumerable<Proveedor> proveedores)
    {
        var listaCompras = compras.ToList();
        return proveedores
            .Where(p => !string.IsNullOrWhiteSpace(p.DiaReparto))
            .Select(p =>
            {
                var comprasProveedor = listaCompras.Where(c => c.ProveedorId == p.Id).ToList();
                var total = comprasProveedor.Count;
                var enDiaEsperado = comprasProveedor.Count(c =>
                    DiaRepartoContiene(p.DiaReparto, NombresDiasSemana[(int)c.Fecha.DayOfWeek]));
                var porcentaje = total > 0 ? (decimal)enDiaEsperado / total * 100 : 0;
                var desviacion = total > 0 && porcentaje < UmbralPorcentajeCumplimientoReparto;

                return new InformeReparto(p.Nombre, p.DiaReparto, total, enDiaEsperado, porcentaje, desviacion);
            })
            .OrderByDescending(r => r.Desviacion)
            .ThenBy(r => r.Proveedor)
            .ToList();
    }

    // ---- 12. Presupuesto vs Real ----

    /// <summary>
    /// Todos los proveedores con al menos una compra en el mes/año elegido. Los que no tengan
    /// presupuesto para ese mes/año no se excluyen: se marcan SinPresupuesto para que la tabla
    /// los muestre igualmente. La gráfica solo incluye a quien tiene los dos valores.
    /// </summary>
    public PresupuestoVsRealResultado CalcularPresupuestoVsReal(
        IEnumerable<Compra> compras, IEnumerable<Presupuesto> presupuestos, int mes, int anio)
    {
        var listaPresupuestos = presupuestos.ToList();
        var comprasDelMes = compras.Where(c => c.Fecha.Year == anio && c.Fecha.Month == mes);
        var gastoReal = AgruparGastoPorProveedorConId(comprasDelMes);

        var filas = gastoReal
            .Select(g =>
            {
                var presupuesto = g.ProveedorId is int proveedorId
                    ? listaPresupuestos.FirstOrDefault(p => p.ProveedorId == proveedorId && p.Mes == mes && p.Anio == anio)
                    : null;
                var porcentajeDesviacion = presupuesto is not null && presupuesto.ImporteAsignado > 0
                    ? (g.Gasto - presupuesto.ImporteAsignado) / presupuesto.ImporteAsignado * 100
                    : (decimal?)null;

                return new PresupuestoVsRealFila(
                    g.Nombre, presupuesto?.ImporteAsignado, g.Gasto, porcentajeDesviacion, presupuesto is null);
            })
            .OrderByDescending(f => f.Real)
            .ToList();

        var conPresupuesto = filas.Where(f => !f.SinPresupuesto).ToList();
        return new PresupuestoVsRealResultado(
            filas,
            conPresupuesto.Select(f => new PuntoDato(f.Proveedor, f.Presupuesto!.Value)).ToList(),
            conPresupuesto.Select(f => new PuntoDato(f.Proveedor, f.Real)).ToList());
    }
}
