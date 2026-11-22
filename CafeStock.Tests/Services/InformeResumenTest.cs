using CafeStock.Back.Models;
using CafeStock.Back.Models.Informes;
using CafeStock.Back.Services.Informes;
using FluentAssertions;

namespace CafeStock.Tests.Services;

/// <summary>
/// Fase 1 de Informes: los cálculos del Resumen (evolución del gasto, inventario, variaciones de precio, cifras y avisos).
/// "Hoy" es el viernes 20/11/2026 en todos los tests (reloj fijo).
/// </summary>
[TestFixture]
public class InformeResumenTest
{
    private sealed class RelojFijo(DateTime ahora) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(DateTime.SpecifyKind(ahora, DateTimeKind.Utc));
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }

    private InformeService _service = null!;

    [SetUp]
    public void Setup() => _service = new InformeService(new RelojFijo(new DateTime(2026, 11, 20, 12, 0, 0)));

    // ---------- Ayudas ----------

    private static DateTime Dia(int mes, int dia, int anio = 2026) => new(anio, mes, dia, 9, 30, 0);

    private static LineaCompra Linea(int productoId, int cantidad, decimal precio, string? nombre = null) =>
        new() { ProductoId = productoId, Cantidad = cantidad, PrecioUnitario = precio, ProductoNombre = nombre };

    private static Compra CompraDe(DateTime fecha, int? proveedorId = 1, string? proveedor = "Leches SA",
        decimal excepcional = 0m, string metodoPago = "", params LineaCompra[] lineas) => new()
    {
        Fecha = fecha, ProveedorId = proveedorId, ProveedorNombre = proveedor, MetodoPago = metodoPago,
        ImporteComprasExcepcionales = excepcional, Lineas = lineas.ToList()
    };

    // ==================== Evolución del gasto mensual ====================

    [Test]
    public void CalcularEvolucionGastoMensual_DevuelveLosUltimosMesesConLosVaciosA0()
    {
        var compras = new[]
        {
            CompraDe(Dia(11, 3), lineas: [Linea(1, 1, 10m)]),
            CompraDe(Dia(11, 18), excepcional: 5m),
            CompraDe(Dia(9, 1), lineas: [Linea(1, 2, 10m)]),
            CompraDe(Dia(11, 1, 2025), lineas: [Linea(1, 1, 99m)])   // fuera de la ventana de 12 meses
        };

        var resultado = _service.CalcularEvolucionGastoMensual(compras);

        resultado.Should().HaveCount(12);
        resultado[0].Etiqueta.Should().Be("dic 25");
        resultado[^1].Should().Be(new PuntoDato("nov 26", 15m));
        resultado.Single(p => p.Etiqueta == "sep 26").Valor.Should().Be(20m);
        resultado.Single(p => p.Etiqueta == "oct 26").Valor.Should().Be(0m);
        resultado.Sum(p => p.Valor).Should().Be(35m);
    }

    [Test]
    public void CalcularEvolucionGastoMensual_ConMesesInvalidos_DevuelveVacio()
    {
        _service.CalcularEvolucionGastoMensual([], 0).Should().BeEmpty();
    }

    // ==================== Inventario ====================

    [Test]
    public void CalcularValorInventario_SumaStockPorPrecioDeReferencia()
    {
        var productos = new[]
        {
            new Producto { StockActual = 3, PrecioUnitario = 2m },
            new Producto { StockActual = 10, PrecioUnitario = 0.5m },
            new Producto { StockActual = 0, PrecioUnitario = 99m }
        };

        _service.CalcularValorInventario(productos).Should().Be(11m);
    }

    [Test]
    public void CalcularProductosAgotados_SoloLosQueTienenMaximoYNoTienenStock()
    {
        var productos = new[]
        {
            new Producto { Nombre = "Leche", StockActual = 0, StockMaximo = 12 },
            new Producto { Nombre = "Azúcar", StockActual = 2, StockMaximo = 8 },
            new Producto { Nombre = "Sin máximo", StockActual = 0, StockMaximo = 0 }
        };

        _service.CalcularProductosAgotados(productos).Select(p => p.Nombre).Should().Equal("Leche");
    }

    // ==================== Variaciones de precio ====================

    [Test]
    public void CalcularVariacionesPrecio_ComparaLaUltimaCompraConLaAnterior()
    {
        var compras = new[]
        {
            CompraDe(Dia(9, 1), lineas: [Linea(1, 10, 1.00m, "Leche")]),
            CompraDe(Dia(10, 1), lineas: [Linea(1, 10, 1.10m, "Leche")]),
            CompraDe(Dia(11, 1), lineas: [Linea(1, 20, 1.20m, "Leche")])   // anterior 1,10 -> 1,20
        };

        var v = _service.CalcularVariacionesPrecio(compras).Should().ContainSingle().Subject;

        v.Producto.Should().Be("Leche");
        v.PrecioAnterior.Should().Be(1.10m);
        v.PrecioActual.Should().Be(1.20m);
        v.PorcentajeVariacion.Should().BeApproximately(9.09m, 0.01m);
        v.ImpactoEuros.Should().Be(2.00m);   // 0,10 × 20 unidades de la última compra: 2 € de más
    }

    [Test]
    public void CalcularVariacionesPrecio_UnaBajadaTieneImpactoNegativo_ElAhorro()
    {
        var compras = new[]
        {
            CompraDe(Dia(10, 1), lineas: [Linea(1, 5, 2.00m, "Café")]),
            CompraDe(Dia(11, 1), lineas: [Linea(1, 5, 1.80m, "Café")])
        };

        var v = _service.CalcularVariacionesPrecio(compras).Single();

        v.PorcentajeVariacion.Should().Be(-10m);
        v.ImpactoEuros.Should().Be(-1.00m);
    }

    [Test]
    public void CalcularVariacionesPrecio_IgnoraProductosConUnaSolaCompraOSinCambio_YOrdenaPorMayorVariacion()
    {
        var compras = new[]
        {
            CompraDe(Dia(10, 1), lineas: [Linea(1, 1, 1m, "A"), Linea(2, 1, 1m, "B"), Linea(3, 1, 1m, "C")]),
            CompraDe(Dia(11, 1), lineas: [Linea(1, 1, 1m, "A"), Linea(2, 1, 1.5m, "B"), Linea(4, 1, 7m, "D")])
        };

        var resultado = _service.CalcularVariacionesPrecio(compras);

        resultado.Select(v => v.Producto).Should().Equal("B");   // A no cambia, C y D solo tienen una compra
    }

    [Test]
    public void CalcularVariacionesPrecio_OrdenaPorMagnitudAbsoluta()
    {
        var compras = new[]
        {
            CompraDe(Dia(10, 1), lineas: [Linea(1, 1, 10m, "Sube poco"), Linea(2, 1, 10m, "Baja mucho")]),
            CompraDe(Dia(11, 1), lineas: [Linea(1, 1, 10.5m, "Sube poco"), Linea(2, 1, 5m, "Baja mucho")])
        };

        _service.CalcularVariacionesPrecio(compras).Select(v => v.Producto)
            .Should().Equal("Baja mucho", "Sube poco");
    }

    // ==================== Resumen ====================

    [Test]
    public void CalcularResumen_ReunePorLasSeisCifras()
    {
        var compras = new[]
        {
            CompraDe(Dia(11, 12), 1, "Alcampo", excepcional: 10m, lineas: [Linea(1, 1, 40m)]),   // 50 en noviembre (hace 8 días)
            CompraDe(Dia(10, 3), 1, "Alcampo", lineas: [Linea(1, 1, 40m)])                      // 40 en octubre
        };
        var presupuestos = new[] { new Presupuesto { ProveedorId = 1, Mes = 11, Anio = 2026, ImporteAsignado = 100m } };
        var proveedores = new[]
        {
            new Proveedor { Id = 1, Nombre = "Alcampo", DiaReparto = "Lunes" },
            new Proveedor { Id = 2, Nombre = "Priégola", DiaReparto = "Martes" }   // nunca se le ha comprado
        };
        var productos = new[]
        {
            new Producto { StockActual = 2, StockMaximo = 4, PrecioUnitario = 3m },
            new Producto { StockActual = 0, StockMaximo = 6, PrecioUnitario = 9m }
        };

        var r = _service.CalcularResumen(compras, presupuestos, proveedores, productos);

        r.GastoMes.Should().Be(50m);
        r.VariacionGastoPorcentual.Should().Be(25m);
        r.PresupuestoMes.Should().Be(100m);
        r.PorcentajePresupuestoConsumido.Should().Be(50m);
        r.ValorInventario.Should().Be(6m);
        r.ProductosAgotados.Should().Be(1);
        r.ProveedoresSinCompra.Should().Be(1);
    }

    [Test]
    public void CalcularResumen_SinPresupuestoEsteMes_NoInventaUnPorcentaje()
    {
        var presupuestoDeOtroMes = new[] { new Presupuesto { ProveedorId = 1, Mes = 10, Anio = 2026, ImporteAsignado = 100m } };

        var r = _service.CalcularResumen([], presupuestoDeOtroMes, [], []);

        r.PresupuestoMes.Should().BeNull();
        r.PorcentajePresupuestoConsumido.Should().BeNull();
        r.GastoMes.Should().Be(0m);
    }

    [Test]
    public void CalcularResumen_ElPorcentajeDePresupuestoSoloMideLosProveedoresPresupuestados()
    {
        var compras = new[]
        {
            CompraDe(Dia(11, 3), 1, "Alcampo", lineas: [Linea(1, 1, 30m)]),       // con presupuesto
            CompraDe(Dia(11, 4), 2, "Sin presupuesto", lineas: [Linea(2, 1, 500m)])  // no cuenta para el %
        };
        var presupuestos = new[] { new Presupuesto { ProveedorId = 1, Mes = 11, Anio = 2026, ImporteAsignado = 60m } };

        var r = _service.CalcularResumen(compras, presupuestos, [], []);

        r.GastoMes.Should().Be(530m);
        r.PorcentajePresupuestoConsumido.Should().Be(50m);
    }

    // ==================== Avisos ====================

    [Test]
    public void CalcularAvisos_PresupuestoSuperado_EsAlertaYVaPrimero()
    {
        var compras = new[] { CompraDe(Dia(11, 3), 1, "Alcampo", lineas: [Linea(1, 1, 120m)]) };
        var presupuestos = new[] { new Presupuesto { ProveedorId = 1, Mes = 11, Anio = 2026, ImporteAsignado = 100m } };
        var proveedores = new[] { new Proveedor { Id = 1, Nombre = "Alcampo" } };
        var productos = new[] { new Producto { Nombre = "Leche", StockActual = 0, StockMaximo = 5 } };

        var avisos = _service.CalcularAvisos(compras, presupuestos, proveedores, productos);

        avisos[0].Nivel.Should().Be(NivelAviso.Alerta);
        avisos[0].Titulo.Should().Contain("Alcampo").And.Contain("presupuesto");
        avisos[0].Detalle.Should().Contain("120,00 €").And.Contain("100,00 €").And.Contain("+20 %");
        avisos.Last().Nivel.Should().Be(NivelAviso.Info);   // los agotados, lo menos grave
    }

    [Test]
    public void CalcularAvisos_SubidaDePrecioDeMasDel5Porciento_Avisa_PeroUnaBajadaONoTanto_No()
    {
        var compras = new[]
        {
            CompraDe(Dia(10, 1), lineas: [Linea(1, 10, 1.00m, "Leche"), Linea(2, 1, 2.00m, "Café"), Linea(3, 1, 1.00m, "Pan")]),
            CompraDe(Dia(11, 1), lineas: [Linea(1, 10, 1.12m, "Leche"), Linea(2, 1, 1.50m, "Café"), Linea(3, 1, 1.02m, "Pan")])
        };

        var avisos = _service.CalcularAvisos(compras, [], [], []);

        var aviso = avisos.Should().ContainSingle().Subject;   // Café baja y Pan sube solo un 2 %: no avisan
        aviso.Nivel.Should().Be(NivelAviso.Atencion);
        aviso.Titulo.Should().Contain("Leche").And.Contain("12 %");
        aviso.Detalle.Should().Contain("1,00 €").And.Contain("1,12 €").And.Contain("1,20 €");
    }

    [Test]
    public void CalcularAvisos_ProveedorSinCompraReciente_Avisa()
    {
        var proveedores = new[] { new Proveedor { Id = 1, Nombre = "Priégola", DiaReparto = "Miércoles" } };
        var compras = new[] { CompraDe(Dia(9, 27), 1, "Priégola", lineas: [Linea(1, 1, 1m)]) };   // hace 54 días

        var aviso = _service.CalcularAvisos(compras, [], proveedores, []).Single();

        aviso.Titulo.Should().Contain("Priégola");
        aviso.Detalle.Should().Contain("54 días");
    }

    [Test]
    public void CalcularAvisos_SinNadaQueAvisar_DevuelveVacio()
    {
        _service.CalcularAvisos([], [], [], []).Should().BeEmpty();
    }

    [Test]
    public void CalcularAvisos_RespetaElMaximo()
    {
        var proveedores = Enumerable.Range(1, 6).Select(i => new Proveedor { Id = i, Nombre = $"P{i}", DiaReparto = "Lunes" }).ToList();

        _service.CalcularAvisos([], [], proveedores, [], maximo: 2).Should().HaveCount(2);
    }
}
