using CafeStock.Back.Models;
using CafeStock.Back.Models.Informes;
using CafeStock.Back.Services.Informes;
using FluentAssertions;

namespace CafeStock.Tests.Services;

/// <summary>
/// Arreglo del gasto en Informes: todos los importes incluyen las compras excepcionales (artículos sueltos sin líneas).
/// "Hoy" es el viernes 20/11/2026 en todos los tests (reloj fijo).
/// </summary>
[TestFixture]
public class InformeGastoComprasExcepcionalesTest
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

    // ==================== El gasto suma las compras excepcionales ====================

    [Test]
    public void ImporteDe_SumaLineasYCompraExcepcional()
    {
        var compra = CompraDe(Dia(11, 3), excepcional: 12.50m, lineas: [Linea(1, 2, 10m)]);

        InformeService.ImporteDe(compra).Should().Be(32.50m);
    }

    [Test]
    public void ImporteDe_CompraSoloExcepcional_NoEsCero()
    {
        InformeService.ImporteDe(CompraDe(Dia(11, 3), excepcional: 18m)).Should().Be(18m);
    }

    [Test]
    public void CalcularGastoMesActual_IncluyeLasComprasExcepcionales()
    {
        var compras = new[]
        {
            CompraDe(Dia(11, 3), excepcional: 15m, lineas: [Linea(1, 1, 10m)]),   // 25 en noviembre
            CompraDe(Dia(10, 20), excepcional: 20m, lineas: [Linea(1, 2, 10m)])   // 40 en octubre
        };

        var resultado = _service.CalcularGastoMesActual(compras);

        resultado.GastoMesActual.Should().Be(25m);
        resultado.VariacionPorcentual.Should().Be(-37.5m);   // (25-40)/40
    }

    [Test]
    public void CalcularGastoPorProveedor_IncluyeLasComprasExcepcionales()
    {
        var compras = new[]
        {
            CompraDe(Dia(11, 3), 1, "Alcampo", excepcional: 30m, lineas: [Linea(1, 1, 10m)]),
            CompraDe(Dia(11, 4), 2, "Priégola", lineas: [Linea(2, 1, 5m)])
        };

        var resultado = _service.CalcularGastoPorProveedor(compras);

        resultado.Should().Equal(new PuntoDato("Alcampo", 40m), new PuntoDato("Priégola", 5m));
    }

    [Test]
    public void CalcularGastoPorMetodoPago_IncluyeLasComprasExcepcionales()
    {
        var compras = new[]
        {
            CompraDe(Dia(11, 3), metodoPago: "Efectivo", excepcional: 7m, lineas: [Linea(1, 1, 3m)]),
            CompraDe(Dia(11, 4), metodoPago: "Efectivo", lineas: [Linea(1, 1, 3m)])
        };

        _service.CalcularGastoPorMetodoPago(compras).Should().ContainSingle()
            .Which.Should().Be(new PuntoDato("Efectivo", 13m));
    }

    [Test]
    public void CalcularGastoPorTipoProveedor_IncluyeLasComprasExcepcionales()
    {
        var proveedores = new[] { new Proveedor { Id = 1, Nombre = "Alcampo", EsSupermercadoGenerico = true } };
        var compras = new[] { CompraDe(Dia(11, 3), 1, "Alcampo", excepcional: 40m, lineas: [Linea(1, 1, 10m)]) };

        _service.CalcularGastoPorTipoProveedor(compras, proveedores).Should().ContainSingle()
            .Which.Valor.Should().Be(50m);
    }

    [Test]
    public void CalcularTicketMedio_UnaCompraSoloExcepcional_CuentaSuImporteYNoUnCero()
    {
        var compras = new[]
        {
            CompraDe(Dia(11, 3), 1, "Alcampo", lineas: [Linea(1, 1, 10m)]),   // 10
            CompraDe(Dia(11, 4), 1, "Alcampo", excepcional: 30m)               // 30, sin líneas
        };

        _service.CalcularTicketMedioPorProveedor(compras).Should().ContainSingle()
            .Which.Valor.Should().Be(20m);   // antes salía 5: contaba la excepcional como 0
    }

    [Test]
    public void CalcularPresupuestoVsReal_IncluyeLasComprasExcepcionales()
    {
        var compras = new[] { CompraDe(Dia(11, 3), 1, "Alcampo", excepcional: 25m, lineas: [Linea(1, 1, 5m)]) };
        var presupuestos = new[] { new Presupuesto { ProveedorId = 1, Mes = 11, Anio = 2026, ImporteAsignado = 20m } };

        var fila = _service.CalcularPresupuestoVsReal(compras, presupuestos, 11, 2026).Filas.Single();

        fila.Real.Should().Be(30m);
        fila.PorcentajeDesviacion.Should().Be(50m);   // (30-20)/20
    }
}
