using CafeStock.Back.Models;
using CafeStock.Back.Repositories.Productos.EfCore;
using CafeStock.Back.Repositories.RegistrosCafe.EfCore;
using CafeStock.Back.Services.Productos;
using CafeStock.Back.Services.RegistrosCafe;
using CafeStock.Back.Validators.Productos;
using FluentAssertions;

namespace CafeStock.Tests.Services;

[TestFixture]
public class CafeEspecialYRegistroTest
{
    private string _dbPath;
    private ProductoService _productos;
    private RegistroCafeService _registros;

    [SetUp]
    public void Setup()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"cafestock_test_{Guid.NewGuid()}.db");
        var cs = $"Data Source={_dbPath}";
        _productos = new ProductoService(new ProductosEfRepository(cs), new ValidadorProducto());
        _registros = new RegistroCafeService(new RegistrosCafeEfRepository(cs), _productos);
    }

    [TearDown]
    public void TearDown()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath))
            File.Delete(_dbPath);
    }

    private Task<CSharpFunctionalExtensions.Result<Producto, CafeStock.Back.Errors.Common.DomainError>> Crear(
        string nombre, decimal actual, decimal maximo, GrupoCafe grupo, FormatoCafe formato) =>
        _productos.CreateAsync(new Producto
        {
            Nombre = nombre, StockActual = actual, StockMaximo = maximo, GrupoCafe = grupo, FormatoCafe = formato
        });

    // ---------- Grupo y formato ----------

    [Test]
    public async Task Create_ConGrupoYFormato_LosGuardaYActivaElSeguimientoIndividual()
    {
        var creado = await Crear("Brasil 250g", 3, 6, GrupoCafe.Casa, FormatoCafe.Cuarto);

        creado.IsSuccess.Should().BeTrue();
        var leido = (await _productos.GetByIdAsync(creado.Value.Id)).Value;
        leido.GrupoCafe.Should().Be(GrupoCafe.Casa);
        leido.FormatoCafe.Should().Be(FormatoCafe.Cuarto);
        leido.SeguimientoIndividual.Should().BeTrue();
    }

    [Test]
    public async Task Create_GrupoSinFormato_EsUnError()
    {
        var creado = await Crear("Brasil", 1, 2, GrupoCafe.Casa, FormatoCafe.SinFormato);

        creado.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task Update_AsignarGrupo_ActivaElSeguimientoIndividual()
    {
        var creado = await Crear("Decaf 1kg", 1, 2, GrupoCafe.SinGrupo, FormatoCafe.SinFormato);

        var actualizado = await _productos.UpdateAsync(creado.Value.Id,
            creado.Value with { GrupoCafe = GrupoCafe.Descafeinado, FormatoCafe = FormatoCafe.Paquete });

        actualizado.IsSuccess.Should().BeTrue();
        actualizado.Value.SeguimientoIndividual.Should().BeTrue();
    }

    // ---------- Rotación del especial ----------

    [Test]
    public async Task Especial_ConElAnteriorConStock_NoPuedeEntrar()
    {
        await Crear("Kenia 250g", 2, 6, GrupoCafe.Especial, FormatoCafe.Cuarto);

        var nuevo = await Crear("Pitalito 250g", 6, 6, GrupoCafe.Especial, FormatoCafe.Cuarto);

        nuevo.IsFailure.Should().BeTrue();
        nuevo.Error.Message.Should().Contain("Kenia 250g");
        (await _productos.GetAllAsync()).Should().NotContain(p => p.Nombre == "Pitalito 250g");
    }

    [Test]
    public async Task Especial_ConElAnteriorAgotado_EntraYElAnteriorDejaDeSerEspecial()
    {
        var kenia = await Crear("Kenia 250g", 0, 6, GrupoCafe.Especial, FormatoCafe.Cuarto);

        var nuevo = await Crear("Pitalito 250g", 6, 6, GrupoCafe.Especial, FormatoCafe.Cuarto);

        nuevo.IsSuccess.Should().BeTrue();
        var anterior = (await _productos.GetByIdAsync(kenia.Value.Id)).Value;
        anterior.GrupoCafe.Should().Be(GrupoCafe.SinGrupo);
        anterior.FormatoCafe.Should().Be(FormatoCafe.SinFormato);
    }

    [Test]
    public async Task Especial_LaRotacionSeComprueba_PorFormato()
    {
        // El cuarto de Kenia sigue con stock, pero el paquete de Kenia está agotado: el paquete nuevo sí puede entrar.
        await Crear("Kenia 250g", 3, 6, GrupoCafe.Especial, FormatoCafe.Cuarto);
        await Crear("Kenia 1kg", 0, 4, GrupoCafe.Especial, FormatoCafe.Paquete);

        var paqueteNuevo = await Crear("Pitalito 1kg", 4, 4, GrupoCafe.Especial, FormatoCafe.Paquete);

        paqueteNuevo.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task Especial_EditarElMismoEspecialConStock_NoSeBloqueaASiMismo()
    {
        var kenia = await Crear("Kenia 250g", 3, 6, GrupoCafe.Especial, FormatoCafe.Cuarto);

        var editado = await _productos.UpdateAsync(kenia.Value.Id, kenia.Value with { StockMaximo = 8 });

        editado.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task Casa_YDescafeinado_NoTienenRotacion()
    {
        await Crear("Brasil A", 3, 6, GrupoCafe.Casa, FormatoCafe.Cuarto);

        var otro = await Crear("Brasil B", 3, 6, GrupoCafe.Casa, FormatoCafe.Cuarto);

        otro.IsSuccess.Should().BeTrue();
    }

    // ---------- Registro: últimos usos y deshacer ----------

    [Test]
    public async Task GetUltimosPorProducto_DevuelveLosMasRecientesDeCadaProducto()
    {
        var a = (await Crear("A", 5, 5, GrupoCafe.Casa, FormatoCafe.Cuarto)).Value;
        var b = (await Crear("B", 5, 5, GrupoCafe.Descafeinado, FormatoCafe.Cuarto)).Value;
        await _registros.RegistrarVentaAsync(a.Id, "Ana");
        await Task.Delay(5);
        await _registros.RegistrarVentaAsync(a.Id, "Luis");
        await Task.Delay(5);
        await _registros.RegistrarVentaAsync(b.Id, "Marta");

        var usos = await _registros.GetUltimosPorProductoAsync([a.Id, b.Id], porProducto: 1);

        usos[a.Id].Select(r => r.NombreTrabajador).Should().Equal("Luis");
        usos[b.Id].Select(r => r.NombreTrabajador).Should().Equal("Marta");
    }

    [Test]
    public async Task DeshacerVenta_DevuelveUnaUnidadYBorraElRegistro()
    {
        var a = (await Crear("A", 3, 5, GrupoCafe.Casa, FormatoCafe.Cuarto)).Value;
        var registro = (await _registros.RegistrarVentaAsync(a.Id, "Ana")).Value;
        (await _productos.GetByIdAsync(a.Id)).Value.StockActual.Should().Be(2);

        var deshecho = await _registros.DeshacerVentaAsync(registro.Id);

        deshecho.IsSuccess.Should().BeTrue();
        (await _productos.GetByIdAsync(a.Id)).Value.StockActual.Should().Be(3);
        (await _registros.GetUltimosAsync()).Should().BeEmpty();
    }

    [Test]
    public async Task DeshacerVenta_DosVeces_NoDevuelveDosUnidades()
    {
        var a = (await Crear("A", 3, 5, GrupoCafe.Casa, FormatoCafe.Cuarto)).Value;
        var registro = (await _registros.RegistrarVentaAsync(a.Id, "Ana")).Value;

        await _registros.DeshacerVentaAsync(registro.Id);
        var segunda = await _registros.DeshacerVentaAsync(registro.Id);

        segunda.IsFailure.Should().BeTrue();
        (await _productos.GetByIdAsync(a.Id)).Value.StockActual.Should().Be(3);
    }
}
