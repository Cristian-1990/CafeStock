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
}
