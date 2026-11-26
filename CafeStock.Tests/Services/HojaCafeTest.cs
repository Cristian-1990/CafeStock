using CafeStock.Back.Models;
using CafeStock.Back.Repositories.HojasCafe.EfCore;
using CafeStock.Back.Repositories.Productos.EfCore;
using CafeStock.Back.Repositories.RegistrosCafe.EfCore;
using CafeStock.Back.Services.HojasCafe;
using CafeStock.Back.Services.Productos;
using CafeStock.Back.Services.RegistrosCafe;
using CafeStock.Back.Validators.Productos;
using FluentAssertions;

namespace CafeStock.Tests.Services;

[TestFixture]
public class HojaCafeTest
{
    private string _dbPath;
    private ProductoService _productos;
    private RegistroCafeService _registros;
    private HojaCafeService _hojas;

    [SetUp]
    public void Setup()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"cafestock_test_{Guid.NewGuid()}.db");
        var cs = $"Data Source={_dbPath}";
        _productos = new ProductoService(new ProductosEfRepository(cs), new ValidadorProducto());
        _registros = new RegistroCafeService(new RegistrosCafeEfRepository(cs), _productos);
        _hojas = new HojaCafeService(new HojasCafeEfRepository(cs), _productos);
    }

    [TearDown]
    public void TearDown()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath))
            File.Delete(_dbPath);
    }

    private async Task<Producto> Cafe(string nombre, GrupoCafe grupo = GrupoCafe.Casa, FormatoCafe formato = FormatoCafe.Paquete,
        decimal stock = 0, decimal maximo = 10)
        => (await _productos.CreateAsync(new Producto { Nombre = nombre, StockActual = stock, StockMaximo = maximo, GrupoCafe = grupo, FormatoCafe = formato })).Value;

    private static NuevoPaqueteCafe[] Paquetes(Producto p, int cuantos, DateTime? tueste = null) =>
        Enumerable.Range(0, cuantos).Select(_ => new NuevoPaqueteCafe(p.Id, tueste)).ToArray();

    // ---------- Lógica pura ----------

    private static PaqueteCafe Paq(int id, int hojaOrigen, string cafe = "Brasil") => new()
    {
        Id = id, ProductoId = 1, ProductoNombre = cafe, Grupo = GrupoCafe.Casa, Formato = FormatoCafe.Paquete, HojaOrigenId = hojaOrigen
    };

    private static RegistroCafe Uso(int id, int paqueteId, int hoja, string quien = "Ana") =>
        new() { Id = id, PaqueteId = paqueteId, HojaId = hoja, NombreTrabajador = quien, Fecha = new DateTime(2026, 11, 1).AddHours(id) };

    [Test]
    public void ConstruirHoja_NoIncluyeLosPaquetesDeHojasPosteriores()
    {
        var tarjetas = TableroCafe.ConstruirHoja(1, [Paq(1, 1), Paq(2, 2)], new Dictionary<int, RegistroCafe>());

        tarjetas.Single().Huecos.Should().HaveCount(1);
    }

    [Test]
    public void ConstruirHoja_UsadoEnEstaHoja_SaleUsadoConSuNombre()
    {
        var tarjetas = TableroCafe.ConstruirHoja(1, [Paq(1, 1), Paq(2, 1)], new Dictionary<int, RegistroCafe> { [1] = Uso(10, 1, 1, "Cristian") });

        var huecos = tarjetas.Single().Huecos;
        huecos[0].Usado.Should().BeTrue();
        huecos[0].Trabajador.Should().Be("Cristian");
        huecos[1].Usado.Should().BeFalse();
    }

    [Test]
    public void ConstruirHoja_SobranteUsadoEnLaSiguiente_EnLaVieja_SaleLibreYMarcadoPasa_EnLaNuevaSaleUsado()
    {
        var paquetes = new[] { Paq(1, 1), Paq(2, 1) };
        var usos = new Dictionary<int, RegistroCafe> { [1] = Uso(10, 1, 1), [2] = Uso(11, 2, 2, "Juanma") };

        var hoja1 = TableroCafe.ConstruirHoja(1, paquetes, usos).Single().Huecos;
        var hoja2 = TableroCafe.ConstruirHoja(2, paquetes, usos).Single().Huecos;

        hoja1.Should().HaveCount(2);
        hoja1[1].Usado.Should().BeFalse();
        hoja1[1].PasaASiguiente.Should().BeTrue();

        // En la hoja 2 el paquete 1 (usado en la hoja 1) ya no está; el 2 sobraba y se usó aquí.
        hoja2.Should().HaveCount(1);
        hoja2[0].Usado.Should().BeTrue();
        hoja2[0].Trabajador.Should().Be("Juanma");
        hoja2[0].Sobrante.Should().BeTrue();
    }

    [Test]
    public void ConstruirHoja_AgrupaPorFormatoYGrupoDelPaquete_NoPorElGrupoActualDelProducto()
    {
        var viejo = Paq(1, 1, "Etiopía") with { Grupo = GrupoCafe.Especial, ProductoId = 5 };
        var casa = Paq(2, 1, "Brasil");

        var tarjetas = TableroCafe.ConstruirHoja(1, [viejo, casa], new Dictionary<int, RegistroCafe>());

        tarjetas.Select(t => (t.ProductoNombre, t.Grupo)).Should().Equal(("Brasil", GrupoCafe.Casa), ("Etiopía", GrupoCafe.Especial));
    }

    // ---------- Recepcionar ----------

    [Test]
    public async Task Recepcionar_PrimeraVez_ReutilizaLaHojaVaciaYSumaStock()
    {
        var brasil = await Cafe("Brasil");

        var hoja = await _hojas.RecepcionarAsync(Paquetes(brasil, 3, new DateTime(2026, 11, 20)), "Cristian");

        hoja.IsSuccess.Should().BeTrue();
        (await _hojas.GetResumenesAsync()).Should().HaveCount(1);
        (await _productos.GetByIdAsync(brasil.Id)).Value.StockActual.Should().Be(3);

        var detalle = (await _hojas.GetDetalleAsync()).Value;
        var huecos = detalle.Tarjetas.Single().Huecos;
        huecos.Should().HaveCount(3);
        huecos[0].Paquete!.RecepcionadoPor.Should().Be("Cristian");
        huecos[0].Paquete!.FechaTueste.Should().Be(new DateTime(2026, 11, 20));
    }

    [Test]
    public async Task Recepcionar_ConHojaConContenido_LaCierraYAbreOtraConLosSobrantes()
    {
        var brasil = await Cafe("Brasil");
        await _hojas.RecepcionarAsync(Paquetes(brasil, 3), "Ana");
        await _registros.RegistrarVentaAsync(brasil.Id, "Cristian");   // usa 1, sobran 2
        var hoja1 = (await _hojas.GetActualAsync()).Id;

        var hoja2 = (await _hojas.RecepcionarAsync(Paquetes(brasil, 5), "Ana")).Value;

        hoja2.Id.Should().NotBe(hoja1);
        var resumenes = await _hojas.GetResumenesAsync();
        resumenes.Should().HaveCount(2);
        resumenes.Single(r => r.Hoja.Id == hoja1).Hoja.Abierta.Should().BeFalse();

        // Hoja nueva: 2 sobrantes + 5 nuevos, ninguno usado, el stock es la suma.
        var nueva = (await _hojas.GetDetalleAsync()).Value.Tarjetas.Single();
        nueva.Total.Should().Be(7);
        nueva.Huecos.Count(h => h.Sobrante).Should().Be(2);
        nueva.Disponibles.Should().Be(7);
        (await _productos.GetByIdAsync(brasil.Id)).Value.StockActual.Should().Be(7);

        // Hoja vieja: sigue como estaba, 3 paquetes y 1 usado por Cristian.
        var vieja = (await _hojas.GetDetalleAsync(hoja1)).Value.Tarjetas.Single();
        vieja.Total.Should().Be(3);
        vieja.Huecos[0].Trabajador.Should().Be("Cristian");
    }

    [Test]
    public async Task UsarUnSobrante_EnLaHojaNueva_LaVieja_LoMuestraLibreMarcadoPasaASiguiente()
    {
        var brasil = await Cafe("Brasil");
        await _hojas.RecepcionarAsync(Paquetes(brasil, 2), "Ana");
        var hoja1 = (await _hojas.GetActualAsync()).Id;
        await _registros.RegistrarVentaAsync(brasil.Id, "Cristian");   // hoja 1: usa 1, sobra 1
        await _hojas.RecepcionarAsync(Paquetes(brasil, 1), "Ana");
        await _registros.RegistrarVentaAsync(brasil.Id, "Juanma");     // usa el sobrante de la hoja 1 (el más antiguo)

        var vieja = (await _hojas.GetDetalleAsync(hoja1)).Value.Tarjetas.Single().Huecos;
        vieja.Should().HaveCount(2);
        vieja.Single(h => !h.Usado).PasaASiguiente.Should().BeTrue();

        var nueva = (await _hojas.GetDetalleAsync()).Value.Tarjetas.Single().Huecos;
        nueva.Should().HaveCount(2); // el sobrante usado + el recién llegado
        nueva.Single(h => h.Usado).Trabajador.Should().Be("Juanma");
        nueva.Single(h => h.Usado).Sobrante.Should().BeTrue();
    }

    [Test]
    public async Task Deshacer_LiberaElPaquete()
    {
        var brasil = await Cafe("Brasil");
        await _hojas.RecepcionarAsync(Paquetes(brasil, 2), "Ana");
        var registro = (await _registros.RegistrarVentaAsync(brasil.Id, "Cristian")).Value;

        await _registros.DeshacerVentaAsync(registro.Id);

        var tarjeta = (await _hojas.GetDetalleAsync()).Value.Tarjetas.Single();
        tarjeta.Usados.Should().Be(0);
        tarjeta.Total.Should().Be(2);
    }

    [Test]
    public async Task Recepcionar_CafeSinGrupo_EsUnError()
    {
        var suelto = await Cafe("Suelto", GrupoCafe.SinGrupo, FormatoCafe.SinFormato);

        var resultado = await _hojas.RecepcionarAsync([new NuevoPaqueteCafe(suelto.Id, null)], "Ana");

        resultado.IsFailure.Should().BeTrue();
    }

    // ---------- Cafés sin clasificar ----------

    [Test]
    public async Task Recepcionar_CafeSinClasificar_LoClasificaAlRecibirlo()
    {
        var nuevo = await Cafe("Kenia Nyota", GrupoCafe.SinClasificar, FormatoCafe.SinFormato);

        var resultado = await _hojas.RecepcionarAsync(
            [new NuevoPaqueteCafe(nuevo.Id, null, GrupoCafe.Especial, FormatoCafe.Cuarto), new NuevoPaqueteCafe(nuevo.Id, null, GrupoCafe.Especial, FormatoCafe.Cuarto)], "Ana");

        resultado.IsSuccess.Should().BeTrue();
        var leido = (await _productos.GetByIdAsync(nuevo.Id)).Value;
        leido.GrupoCafe.Should().Be(GrupoCafe.Especial);
        leido.FormatoCafe.Should().Be(FormatoCafe.Cuarto);
        leido.SeguimientoIndividual.Should().BeTrue();
        leido.StockActual.Should().Be(2);
        var tarjeta = (await _hojas.GetDetalleAsync()).Value.Tarjetas.Single();
        (tarjeta.Grupo, tarjeta.Formato, tarjeta.Total).Should().Be((GrupoCafe.Especial, FormatoCafe.Cuarto, 2));
    }

    [Test]
    public async Task Recepcionar_CafeSinClasificar_SinGrupoElegido_EsUnErrorYNoLoToca()
    {
        var nuevo = await Cafe("Kenia Nyota", GrupoCafe.SinClasificar, FormatoCafe.SinFormato);

        var resultado = await _hojas.RecepcionarAsync([new NuevoPaqueteCafe(nuevo.Id, null)], "Ana");

        resultado.IsFailure.Should().BeTrue();
        (await _productos.GetByIdAsync(nuevo.Id)).Value.GrupoCafe.Should().Be(GrupoCafe.SinClasificar);
        (await _hojas.GetDetalleAsync()).Value.Tarjetas.Should().BeEmpty();
    }

    [Test]
    public async Task Recepcionar_EspecialNuevoConOtroEspecialConPaquetes_RespetaLaRotacion()
    {
        var primero = await Cafe("Etiopía", GrupoCafe.Especial, FormatoCafe.Cuarto);
        await _hojas.RecepcionarAsync(Paquetes(primero, 2), "Ana");
        var nuevo = await Cafe("Kenia", GrupoCafe.SinClasificar, FormatoCafe.SinFormato);

        var resultado = await _hojas.RecepcionarAsync([new NuevoPaqueteCafe(nuevo.Id, null, GrupoCafe.Especial, FormatoCafe.Cuarto)], "Ana");

        resultado.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task Recepcionar_SinPaquetesOSinQuien_EsUnError()
    {
        var brasil = await Cafe("Brasil");

        (await _hojas.RecepcionarAsync([], "Ana")).IsFailure.Should().BeTrue();
        (await _hojas.RecepcionarAsync(Paquetes(brasil, 1), " ")).IsFailure.Should().BeTrue();
        (await _hojas.RecepcionarAsync(Paquetes(brasil, 1, DateTime.Today.AddDays(3)), "Ana")).IsFailure.Should().BeTrue();
    }

    // ---------- Café anterior a las hojas ----------

    [Test]
    public async Task CafeConStockYUsosAnteriores_SeConvierteEnPaquetesDeLaPrimeraHoja()
    {
        var brasil = await Cafe("Brasil", stock: 3, maximo: 5);
        // Se usa antes de que existan paquetes: el repositorio siembra los paquetes al registrar.
        await _registros.RegistrarVentaAsync(brasil.Id, "Cristian");

        var tarjeta = (await _hojas.GetDetalleAsync()).Value.Tarjetas.Single();

        tarjeta.Total.Should().Be(3);          // los 3 del stock inicial, uno ya usado
        tarjeta.Usados.Should().Be(1);
        tarjeta.Huecos[0].Trabajador.Should().Be("Cristian");
        tarjeta.Huecos[0].Paquete!.RecepcionadoPor.Should().Be("Hoja inicial");
    }

    [Test]
    public async Task GetDetalle_HojaInexistente_EsUnError()
    {
        (await _hojas.GetDetalleAsync(999)).IsFailure.Should().BeTrue();
    }
}
