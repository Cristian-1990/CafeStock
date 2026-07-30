using CafeStock.Back.Models;
using CafeStock.Back.Repositories.Productos.EfCore;
using FluentAssertions;

namespace CafeStock.Tests.Repositories;

[TestFixture]
public class ProductosEfRepositoryTest
{
    private string _dbPath;
    private string _connectionString;
    private ProductosEfRepository _repository;

    [SetUp]
    public void Setup()
    {
        // Base de datos temporal única por test
        _dbPath = Path.Combine(Path.GetTempPath(), $"cafestock_test_{Guid.NewGuid()}.db");
        _connectionString = $"Data Source={_dbPath}";
        _repository = new ProductosEfRepository(_connectionString);
    }

    [TearDown]
    public void TearDown()
    {
        // Fuerza a SQLite a soltar el archivo antes de borrarlo
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

        if (File.Exists(_dbPath))
            File.Delete(_dbPath);
    }

    [Test]
    public async Task CreateAsync_GuardaProducto()
    {
        // Arrange
        var producto = new Producto { Nombre = "Café", StockActual = 2, StockMaximo = 5 };

        // Act
        var resultado = await _repository.CreateAsync(producto);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Id.Should().BeGreaterThan(0);
    }

    [Test]
    public async Task GetByIdAsync_ProductoExiste_LoDevuelve()
    {
        // Arrange
        var creado = await _repository.CreateAsync(
            new Producto { Nombre = "Café", StockActual = 2, StockMaximo = 5 });

        // Act
        var resultado = await _repository.GetByIdAsync(creado.Value.Id);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Nombre.Should().Be("Café");
    }

    [Test]
    public async Task GetByIdAsync_ProductoNoExiste_DevuelveFailure()
    {
        // Act
        var resultado = await _repository.GetByIdAsync(999);

        // Assert
        resultado.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task GetAllAsync_DevuelveTodos()
    {
        // Arrange
        await _repository.CreateAsync(new Producto { Nombre = "Café", StockActual = 2, StockMaximo = 5 });
        await _repository.CreateAsync(new Producto { Nombre = "Azúcar", StockActual = 1, StockMaximo = 3 });

        // Act
        var productos = await _repository.GetAllAsync();

        // Assert
        productos.Should().HaveCount(2);
    }

    [Test]
    public async Task UpdateAsync_ModificaProducto()
    {
        // Arrange
        var creado = await _repository.CreateAsync(
            new Producto { Nombre = "Café", StockActual = 2, StockMaximo = 5 });
        var modificado = creado.Value with { StockActual = 4 };

        // Act
        var resultado = await _repository.UpdateAsync(creado.Value.Id, modificado);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.StockActual.Should().Be(4);
    }

    [Test]
    public async Task DeleteAsync_EliminaProducto()
    {
        // Arrange
        var creado = await _repository.CreateAsync(
            new Producto { Nombre = "Café", StockActual = 2, StockMaximo = 5 });

        // Act
        var resultado = await _repository.DeleteAsync(creado.Value.Id);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        var comprobar = await _repository.GetByIdAsync(creado.Value.Id);
        comprobar.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task ProductosUrgentes_DevuelveSoloBajoMinimo()
    {
        // Arrange
        await _repository.CreateAsync(new Producto { Nombre = "Café", StockActual = 2, StockMaximo = 5 });   // falta
        await _repository.CreateAsync(new Producto { Nombre = "Sal", StockActual = 5, StockMaximo = 5 });    // completo

        // Act
        var urgentes = await _repository.ProductosUrgentes();

        // Assert
        urgentes.Should().HaveCount(1);
        urgentes.First().Nombre.Should().Be("Café");
    }
}