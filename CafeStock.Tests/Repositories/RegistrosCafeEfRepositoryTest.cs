using CafeStock.Back.Models;
using CafeStock.Back.Repositories.Productos.EfCore;
using CafeStock.Back.Repositories.RegistrosCafe.EfCore;
using FluentAssertions;

namespace CafeStock.Tests.Repositories;

[TestFixture]
public class RegistrosCafeEfRepositoryTest
{
    private string _dbPath;
    private string _connectionString;
    private RegistrosCafeEfRepository _repository;
    private ProductosEfRepository _productosRepository;

    [SetUp]
    public void Setup()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"cafestock_test_{Guid.NewGuid()}.db");
        _connectionString = $"Data Source={_dbPath}";
        _repository = new RegistrosCafeEfRepository(_connectionString);
        _productosRepository = new ProductosEfRepository(_connectionString);
    }

    [TearDown]
    public void TearDown()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath))
            File.Delete(_dbPath);
    }

    [Test]
    public async Task RegistrarVentaAsync_ConStock_RestaUnoYCreaElRegistro()
    {
        // Arrange
        var creado = await _productosRepository.CreateAsync(
            new Producto { Nombre = "Brasil — Cuarto 250g", StockActual = 3, StockMaximo = 4, SeguimientoIndividual = true });

        // Act
        var resultado = await _repository.RegistrarVentaAsync(creado.Value.Id, "Ana");

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.NombreTrabajador.Should().Be("Ana");
        resultado.Value.ProductoId.Should().Be(creado.Value.Id);

        var producto = await _productosRepository.GetByIdAsync(creado.Value.Id);
        producto.Value.StockActual.Should().Be(2);
    }

    [Test]
    public async Task RegistrarVentaAsync_SinStock_DevuelveFailureYNoCreaRegistro()
    {
        // Arrange
        var creado = await _productosRepository.CreateAsync(
            new Producto { Nombre = "Brasil — Cuarto 250g", StockActual = 0, StockMaximo = 4, SeguimientoIndividual = true });

        // Act
        var resultado = await _repository.RegistrarVentaAsync(creado.Value.Id, "Ana");

        // Assert
        resultado.IsFailure.Should().BeTrue();
        var historial = await _repository.GetUltimosAsync(10);
        historial.Should().BeEmpty();
    }

    [Test]
    public async Task RegistrarVentaAsync_ProductoNoExiste_DevuelveFailure()
    {
        // Act
        var resultado = await _repository.RegistrarVentaAsync(999, "Ana");

        // Assert
        resultado.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task GetUltimosAsync_DevuelveDelMasRecienteAlMasAntiguoYRespetaElLimite()
    {
        // Arrange
        var creado = await _productosRepository.CreateAsync(
            new Producto { Nombre = "Brasil — Cuarto 250g", StockActual = 5, StockMaximo = 5, SeguimientoIndividual = true });
        await _repository.RegistrarVentaAsync(creado.Value.Id, "Ana");
        await Task.Delay(10);
        await _repository.RegistrarVentaAsync(creado.Value.Id, "Bea");
        await Task.Delay(10);
        await _repository.RegistrarVentaAsync(creado.Value.Id, "Cris");

        // Act
        var ultimos = (await _repository.GetUltimosAsync(2)).ToList();

        // Assert
        ultimos.Should().HaveCount(2);
        ultimos[0].NombreTrabajador.Should().Be("Cris");
        ultimos[1].NombreTrabajador.Should().Be("Bea");
    }

    [Test]
    public async Task RegistrarVentaAsync_VentasSimultaneas_NoPierdeNingunDescuento()
    {
        // Arrange: 20 unidades y 20 tablets vendiendo "a la vez". Cada venta lee el stock,
        // resta 1 y guarda; sin control de concurrencia dos lecturas del mismo valor
        // producen un único descuento (lost update) y el stock final queda por encima de 0.
        const int unidades = 20;
        var creado = await _productosRepository.CreateAsync(
            new Producto { Nombre = "Brasil — Cuarto 250g", StockActual = unidades, StockMaximo = unidades, SeguimientoIndividual = true });
        var id = creado.Value.Id;

        // Act: un repositorio por "tablet" y todas arrancando a la vez.
        var salida = new TaskCompletionSource();
        var ventas = Enumerable.Range(0, unidades).Select(async i =>
        {
            var repo = new RegistrosCafeEfRepository(_connectionString);
            await salida.Task;
            return await repo.RegistrarVentaAsync(id, $"Trabajador{i}");
        }).ToList();
        salida.SetResult();
        var resultados = await Task.WhenAll(ventas);

        // Assert: todas las ventas tienen éxito, el stock llega a 0 y hay 20 registros.
        resultados.Count(r => r.IsSuccess).Should().Be(unidades);
        (await _productosRepository.GetByIdAsync(id)).Value.StockActual.Should().Be(0);
        (await _repository.GetUltimosAsync(100)).Should().HaveCount(unidades);
    }

    [Test]
    public async Task RegistrarVentaAsync_MasVentasSimultaneasQueStock_NuncaDejaElStockNegativo()
    {
        // Arrange: 5 unidades y 12 ventas a la vez; solo 5 pueden tener éxito.
        var creado = await _productosRepository.CreateAsync(
            new Producto { Nombre = "Brasil — Cuarto 250g", StockActual = 5, StockMaximo = 5, SeguimientoIndividual = true });
        var id = creado.Value.Id;

        // Act
        var salida = new TaskCompletionSource();
        var ventas = Enumerable.Range(0, 12).Select(async i =>
        {
            var repo = new RegistrosCafeEfRepository(_connectionString);
            await salida.Task;
            return await repo.RegistrarVentaAsync(id, $"Trabajador{i}");
        }).ToList();
        salida.SetResult();
        var resultados = await Task.WhenAll(ventas);

        // Assert
        resultados.Count(r => r.IsSuccess).Should().Be(5);
        (await _productosRepository.GetByIdAsync(id)).Value.StockActual.Should().Be(0);
        (await _repository.GetUltimosAsync(100)).Should().HaveCount(5);
    }
}
