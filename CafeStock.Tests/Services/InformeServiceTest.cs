using CafeStock.Back.Models;
using CafeStock.Back.Models.Informes;
using CafeStock.Back.Services.Informes;
using FluentAssertions;

namespace CafeStock.Tests.Services;

/// <summary>
/// "Hoy" es el viernes 20/11/2026 a mediodía en todos los tests (reloj fijo), así que la semana
/// en curso empieza el lunes 16/11/2026, el mes en curso el 01/11/2026 y el año el 01/01/2026.
/// </summary>
[TestFixture]
public class InformeServiceTest
{
    private sealed class RelojFijo(DateTime ahora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(DateTime.SpecifyKind(ahora, DateTimeKind.Utc));
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }

    private InformeService _service = null!;

    [SetUp]
    public void Setup()
    {
        _service = new InformeService(new RelojFijo(new DateTime(2026, 11, 20, 12, 0, 0)));
    }

    // ---------- Ayudas ----------

    private static Compra CompraDe(DateTime fecha, int? proveedorId = 1, string? proveedor = "Leches SA",
        string metodoPago = "", params LineaCompra[] lineas) => new()
    {
        Fecha = fecha, ProveedorId = proveedorId, ProveedorNombre = proveedor, MetodoPago = metodoPago,
        Lineas = lineas.ToList()
    };

    private static LineaCompra Linea(int productoId, int cantidad, decimal precio, string? nombre = null) =>
        new() { ProductoId = productoId, Cantidad = cantidad, PrecioUnitario = precio, ProductoNombre = nombre };

    private static DateTime Dia(int mes, int dia, int anio = 2026) => new(anio, mes, dia, 9, 30, 0);

    // ---------- 1. Gasto del mes actual ----------

    [Test]
    public void CalcularGastoMesActual_SumaElMesActualYCalculaLaVariacionFrenteAlAnterior()
    {
        var compras = new[]
        {
            CompraDe(Dia(11, 3), lineas: [Linea(1, 2, 10m), Linea(2, 1, 5m)]),   // 25 en noviembre
            CompraDe(Dia(11, 18), lineas: [Linea(1, 1, 25m)]),                    // +25 = 50
            CompraDe(Dia(10, 20), lineas: [Linea(1, 4, 10m)]),                    // 40 en octubre
            CompraDe(Dia(9, 1), lineas: [Linea(1, 100, 10m)])                     // no cuenta
        };

        var resultado = _service.CalcularGastoMesActual(compras);

        resultado.GastoMesActual.Should().Be(50m);
        resultado.VariacionPorcentual.Should().Be(25m); // (50-40)/40*100
    }

    [Test]
    public void CalcularGastoMesActual_SinGastoElMesAnterior_VariacionEsNull()
    {
        var compras = new[] { CompraDe(Dia(11, 3), lineas: [Linea(1, 1, 10m)]) };

        var resultado = _service.CalcularGastoMesActual(compras);

        resultado.GastoMesActual.Should().Be(10m);
        resultado.VariacionPorcentual.Should().BeNull();
    }

    // ---------- 2. Filtros y gasto por proveedor ----------

    private static Compra[] ComprasParaFiltros() =>
    [
        CompraDe(new DateTime(2026, 11, 20, 23, 59, 0), lineas: [Linea(1, 1, 1m)]),  // hoy (la hora no importa)
        CompraDe(Dia(11, 16), lineas: [Linea(1, 1, 1m)]),                            // lunes de esta semana
        CompraDe(Dia(11, 15), lineas: [Linea(1, 1, 1m)]),                            // domingo anterior
        CompraDe(Dia(11, 1), lineas: [Linea(1, 1, 1m)]),                             // primer día del mes
        CompraDe(Dia(10, 31), lineas: [Linea(1, 1, 1m)]),                            // mes anterior
        CompraDe(Dia(1, 1), lineas: [Linea(1, 1, 1m)]),                              // primer día del año
        CompraDe(Dia(12, 31, 2025), lineas: [Linea(1, 1, 1m)])                       // año anterior
    ];

    [TestCase(RangoFechas.UltimaSemana, 2)]
    [TestCase(RangoFechas.UltimoMes, 4)]
    [TestCase(RangoFechas.UltimoAno, 6)]
    [TestCase(RangoFechas.Todo, 7)]
    public void FiltrarCompras_ChipsDeCalendarioReal(RangoFechas rango, int esperadas)
    {
        var resultado = _service.FiltrarCompras(ComprasParaFiltros(), rango, null, null, null).ToList();

        resultado.Should().HaveCount(esperadas);
    }

    [Test]
    public void FiltrarCompras_RangoLibreCompleto_TienePrioridadSobreElChip()
    {
        // El chip "Última semana" daría 2, pero el rango libre 1/11–15/11 incluye 2 compras distintas.
        var resultado = _service.FiltrarCompras(
            ComprasParaFiltros(), RangoFechas.UltimaSemana, Dia(11, 1), Dia(11, 15), null).ToList();

        resultado.Select(c => c.Fecha.Day).Should().BeEquivalentTo([1, 15]);
    }

    [Test]
    public void FiltrarCompras_RangoLibreIncompleto_SigueAplicandoElChip()
    {
        var resultado = _service.FiltrarCompras(
            ComprasParaFiltros(), RangoFechas.UltimaSemana, Dia(11, 1), null, null).ToList();

        resultado.Should().HaveCount(2);
    }

    [Test]
    public void FiltrarCompras_ConProveedor_SoloDevuelveEsteProveedor()
    {
        var compras = new[]
        {
            CompraDe(Dia(11, 3), proveedorId: 1), CompraDe(Dia(11, 4), proveedorId: 2), CompraDe(Dia(11, 5), proveedorId: 2)
        };

        var resultado = _service.FiltrarCompras(compras, RangoFechas.Todo, null, null, 2).ToList();

        resultado.Should().HaveCount(2).And.OnlyContain(c => c.ProveedorId == 2);
    }

    [Test]
    public void HayRangoLibre_SoloConAmbasFechas()
    {
        _service.HayRangoLibre(Dia(1, 1), Dia(1, 2)).Should().BeTrue();
        _service.HayRangoLibre(Dia(1, 1), null).Should().BeFalse();
        _service.HayRangoLibre(null, null).Should().BeFalse();
    }

    [Test]
    public void CalcularGastoPorProveedor_AgrupaPorProveedorYOrdenaDeMayorAMenor()
    {
        var compras = new[]
        {
            CompraDe(Dia(11, 3), 1, "Leches SA", lineas: [Linea(1, 2, 10m)]),   // 20
            CompraDe(Dia(11, 4), 2, "Pan SL", lineas: [Linea(2, 1, 50m)]),      // 50
            CompraDe(Dia(11, 5), 1, "Leches SA", lineas: [Linea(1, 1, 5m)]),    // +5 = 25
            CompraDe(Dia(11, 6), null, null, lineas: [Linea(3, 1, 1m)])         // sin proveedor
        };

        var resultado = _service.CalcularGastoPorProveedor(compras);

        resultado.Should().Equal(
            new PuntoDato("Pan SL", 50m), new PuntoDato("Leches SA", 25m), new PuntoDato("Sin proveedor asignado", 1m));
    }

    // ---------- 3. Evolución de precio ----------

    [Test]
    public void CalcularEvolucionPrecio_SinProducto_DevuelveVacio()
    {
        _service.CalcularEvolucionPrecio([CompraDe(Dia(11, 3), lineas: [Linea(1, 1, 2m)])], null).Should().BeEmpty();
    }

    [Test]
    public void CalcularEvolucionPrecio_OrdenaPorFechaYSoloIncluyeElProducto()
    {
        var compras = new[]
        {
            CompraDe(Dia(11, 10), lineas: [Linea(1, 1, 3m), Linea(2, 1, 99m)]),
            CompraDe(Dia(11, 2), lineas: [Linea(1, 1, 2m)])
        };

        var resultado = _service.CalcularEvolucionPrecio(compras, 1);

        resultado.Should().Equal(new PuntoDato("02/11/26", 2m), new PuntoDato("10/11/26", 3m));
    }

    // ---------- 4 y 5. Productos y método de pago ----------

    [Test]
    public void CalcularProductosPorGastoTotal_DevuelveSoloLos10MasCaros()
    {
        var lineas = Enumerable.Range(1, 12).Select(i => Linea(i, 1, i, $"P{i}")).ToArray();

        var resultado = _service.CalcularProductosPorGastoTotal([CompraDe(Dia(11, 3), lineas: lineas)]);

        resultado.Should().HaveCount(10);
        resultado[0].Should().Be(new PuntoDato("P12", 12m));
        resultado.Should().NotContain(p => p.Etiqueta == "P1" || p.Etiqueta == "P2");
    }

    [Test]
    public void CalcularProductosPorGastoTotal_SinNombre_UsaElIdComoEtiqueta()
    {
        var resultado = _service.CalcularProductosPorGastoTotal([CompraDe(Dia(11, 3), lineas: [Linea(7, 1, 1m)])]);

        resultado.Single().Etiqueta.Should().Be("Producto #7");
    }

    [Test]
    public void CalcularGastoPorMetodoPago_AgrupaYUsaSinEspecificarParaVacios()
    {
        var compras = new[]
        {
            CompraDe(Dia(11, 3), metodoPago: "Efectivo", lineas: [Linea(1, 1, 10m)]),
            CompraDe(Dia(11, 4), metodoPago: "Efectivo", lineas: [Linea(1, 1, 5m)]),
            CompraDe(Dia(11, 5), metodoPago: "  ", lineas: [Linea(1, 1, 30m)])
        };

        var resultado = _service.CalcularGastoPorMetodoPago(compras);

        resultado.Should().Equal(new PuntoDato("Sin especificar", 30m), new PuntoDato("Efectivo", 15m));
    }

    // ---------- 6. Proveedores sin compra reciente ----------

    [Test]
    public void CalcularProveedoresSinCompraReciente_AvisaDeLosQueSuperanElUmbralOQueNuncaCompraron()
    {
        var proveedores = new[]
        {
            new Proveedor { Id = 1, Nombre = "Reciente", DiaReparto = "Lunes" },       // 5 días: no avisa
            new Proveedor { Id = 2, Nombre = "Justo", DiaReparto = "Lunes" },          // 14 días exactos: no avisa
            new Proveedor { Id = 3, Nombre = "Antiguo", DiaReparto = "Lunes" },        // 15 días: avisa
            new Proveedor { Id = 4, Nombre = "Nunca", DiaReparto = "Lunes" },          // sin compras: avisa primero
            new Proveedor { Id = 5, Nombre = "SinReparto", DiaReparto = "" }           // sin reparto: se ignora
        };
        var compras = new[]
        {
            CompraDe(new DateTime(2026, 11, 15, 12, 0, 0), 1),
            CompraDe(new DateTime(2026, 11, 6, 12, 0, 0), 2),
            CompraDe(new DateTime(2026, 11, 5, 12, 0, 0), 3)
        };

        var resultado = _service.CalcularProveedoresSinCompraReciente(compras, proveedores);

        resultado.Should().Equal(new ProveedorAlerta("Nunca", null), new ProveedorAlerta("Antiguo", 15));
    }

    // ---------- 7. PPV ----------

    [Test]
    public void CalcularPpvPorProducto_CalculaDesviacionYPrecioMedioPonderado()
    {
        var productos = new[]
        {
            new Producto { Id = 1, Nombre = "Leche", PrecioUnitario = 1m },
            new Producto { Id = 2, Nombre = "SinReferencia", PrecioUnitario = 0m }
        };
        var compras = new[]
        {
            CompraDe(Dia(11, 3), lineas: [Linea(1, 1, 2m), Linea(2, 5, 9m)]),   // +1 sobre la referencia
            CompraDe(Dia(11, 4), lineas: [Linea(1, 3, 1m)])                      // a referencia
        };

        var resultado = _service.CalcularPpvPorProducto(compras, productos);

        // PPV = (2-1)*1 + (1-1)*3 = 1. Medio ponderado = (2*1+1*3)/4 = 1,25 → +25 % sobre la referencia.
        resultado.Should().ContainSingle().Which.Should().Be(new PuntoDatoPpv("Leche", 1m, 25m));
    }

    // ---------- 8. Genérico vs habituales ----------

    [Test]
    public void CalcularGastoPorTipoProveedor_SeparaGenericosDeHabitualesYExcluyeComprasSinProveedor()
    {
        var proveedores = new[]
        {
            new Proveedor { Id = 1, Nombre = "Alcampo", EsSupermercadoGenerico = true },
            new Proveedor { Id = 2, Nombre = "Leches", EsSupermercadoGenerico = false }
        };
        var compras = new[]
        {
            CompraDe(Dia(11, 3), 1, lineas: [Linea(1, 1, 10m)]),
            CompraDe(Dia(11, 4), 2, lineas: [Linea(1, 1, 40m)]),
            CompraDe(Dia(11, 5), null, null, lineas: [Linea(1, 1, 999m)]),
            CompraDe(Dia(11, 6), 77, "Borrado", lineas: [Linea(1, 1, 999m)])
        };

        var resultado = _service.CalcularGastoPorTipoProveedor(compras, proveedores);

        resultado.Should().Equal(new PuntoDato("Proveedores habituales", 40m), new PuntoDato("Supermercado genérico", 10m));
    }

    // ---------- 9. Concentración ----------

    [Test]
    public void CalcularConcentracionGastoPorProveedor_RepartePorcentajesQueSuman100()
    {
        var gasto = new List<PuntoDato> { new("A", 75m), new("B", 25m) };

        var resultado = _service.CalcularConcentracionGastoPorProveedor(gasto);

        resultado.Should().Equal(new PuntoDato("A", 75m), new PuntoDato("B", 25m));
    }

    [Test]
    public void CalcularConcentracionGastoPorProveedor_SinGasto_DevuelveVacio()
    {
        _service.CalcularConcentracionGastoPorProveedor([new PuntoDato("A", 0m)]).Should().BeEmpty();
        _service.CalcularConcentracionGastoPorProveedor([]).Should().BeEmpty();
    }

    // ---------- 10. Ticket medio ----------

    [Test]
    public void CalcularTicketMedioPorProveedor_PromediaElImporteDeCadaCompra()
    {
        var compras = new[]
        {
            CompraDe(Dia(11, 3), 1, "Leches SA", lineas: [Linea(1, 1, 10m)]),   // 10
            CompraDe(Dia(11, 4), 1, "Leches SA", lineas: [Linea(1, 1, 30m)]),   // 30  → media 20
            CompraDe(Dia(11, 5), 2, "Pan SL", lineas: [Linea(2, 1, 50m)])
        };

        var resultado = _service.CalcularTicketMedioPorProveedor(compras);

        resultado.Should().Equal(new PuntoDato("Pan SL", 50m), new PuntoDato("Leches SA", 20m));
    }

    // ---------- 10b. Excedentes de stock ----------

    [Test]
    public void CalcularRecepcionesConExcedente_SoloLasQueSuperanElMaximo()
    {
        var superado = Linea(1, 6, 1m, "Leche"); superado.StockResultanteTrasRecepcion = 12m; superado.StockMaximoEnMomento = 10m;
        var justo = Linea(2, 3, 1m, "Pan"); justo.StockResultanteTrasRecepcion = 10m; justo.StockMaximoEnMomento = 10m;
        var sinDato = Linea(3, 1, 1m, "Antigua");   // línea anterior al campo: ambos null
        var mediaDato = Linea(4, 1, 1m, "Media"); mediaDato.StockResultanteTrasRecepcion = 50m;

        var resultado = _service.CalcularRecepcionesConExcedente(
            [CompraDe(Dia(11, 3), lineas: [superado, justo, sinDato, mediaDato])]);

        resultado.Should().ContainSingle().Which.Should().Be(
            new LineaExcedenteStock(Dia(11, 3), "Leche", "Leches SA", 6, 2m));
    }

    // ---------- 11. Frecuencia de reparto ----------

    [Test]
    public void CalcularFrecuenciaRepartoVsEsperado_IgnoraTildesYMayusculasYMarcaLaDesviacion()
    {
        var proveedores = new[]
        {
            // 4 compras, 3 en miércoles (11/11/2026 es miércoles): 75 % → cumple
            new Proveedor { Id = 1, Nombre = "Cumple", DiaReparto = "MIERCOLES 9:00" },
            // 2 compras, ninguna en lunes: 0 % → desviación
            new Proveedor { Id = 2, Nombre = "Desvía", DiaReparto = "Lunes" },
            // día múltiple en texto libre
            new Proveedor { Id = 3, Nombre = "Multi", DiaReparto = "Martes, Miércoles y Jueves" },
            // sin compras: no es desviación
            new Proveedor { Id = 4, Nombre = "SinCompras", DiaReparto = "Viernes" },
            new Proveedor { Id = 5, Nombre = "SinReparto", DiaReparto = "" }
        };
        var compras = new[]
        {
            CompraDe(Dia(11, 4), 1), CompraDe(Dia(11, 11), 1), CompraDe(Dia(11, 18), 1), CompraDe(Dia(11, 5), 1), // mié×3, jue
            CompraDe(Dia(11, 4), 2), CompraDe(Dia(11, 5), 2),                                                     // mié, jue
            CompraDe(Dia(11, 3), 3)                                                                               // martes
        };

        var resultado = _service.CalcularFrecuenciaRepartoVsEsperado(compras, proveedores);

        resultado.Select(r => r.Proveedor).Should().Equal("Desvía", "Cumple", "Multi", "SinCompras");
        resultado[0].Should().Be(new InformeReparto("Desvía", "Lunes", 2, 0, 0m, true));
        resultado[1].Should().Be(new InformeReparto("Cumple", "MIERCOLES 9:00", 4, 3, 75m, false));
        resultado[2].ComprasEnDiaEsperado.Should().Be(1);
        resultado[3].Should().Be(new InformeReparto("SinCompras", "Viernes", 0, 0, 0m, false));
    }

    // ---------- 12. Presupuesto vs real ----------

    [Test]
    public void CalcularPresupuestoVsReal_CruzaConElPresupuestoDelMesYMarcaLosQueNoTienen()
    {
        var compras = new[]
        {
            CompraDe(Dia(11, 3), 1, "Leches SA", lineas: [Linea(1, 1, 120m)]),    // presupuesto 100 → +20 %
            CompraDe(Dia(11, 4), 2, "Pan SL", lineas: [Linea(2, 1, 30m)]),         // sin presupuesto
            CompraDe(Dia(10, 4), 1, "Leches SA", lineas: [Linea(1, 1, 999m)])      // otro mes: se ignora
        };
        var presupuestos = new[]
        {
            new Presupuesto { ProveedorId = 1, Mes = 11, Anio = 2026, ImporteAsignado = 100m },
            new Presupuesto { ProveedorId = 1, Mes = 10, Anio = 2026, ImporteAsignado = 1m }   // otro mes
        };

        var resultado = _service.CalcularPresupuestoVsReal(compras, presupuestos, 11, 2026);

        resultado.Filas.Should().Equal(
            new PresupuestoVsRealFila("Leches SA", 100m, 120m, 20m, false),
            new PresupuestoVsRealFila("Pan SL", null, 30m, null, true));
        // La gráfica solo incluye a quien tiene los dos valores.
        resultado.SeriePresupuesto.Should().Equal(new PuntoDato("Leches SA", 100m));
        resultado.SerieReal.Should().Equal(new PuntoDato("Leches SA", 120m));
    }

    [Test]
    public void CalcularPresupuestoVsReal_PresupuestoCero_NoCalculaDesviacion()
    {
        var compras = new[] { CompraDe(Dia(11, 3), 1, "Leches SA", lineas: [Linea(1, 1, 10m)]) };
        var presupuestos = new[] { new Presupuesto { ProveedorId = 1, Mes = 11, Anio = 2026, ImporteAsignado = 0m } };

        var resultado = _service.CalcularPresupuestoVsReal(compras, presupuestos, 11, 2026);

        resultado.Filas.Single().PorcentajeDesviacion.Should().BeNull();
    }
}
