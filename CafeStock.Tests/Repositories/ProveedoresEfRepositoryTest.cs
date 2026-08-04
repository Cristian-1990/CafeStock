using CafeStock.Back.Models;
using CafeStock.Back.Repositories.Productos.EfCore;
using CafeStock.Back.Repositories.Proveedores.EfCore;
using FluentAssertions;

namespace CafeStock.Tests.Repositories;

[TestFixture]
public class ProveedoresEfRepositoryTest
{
    private string _dbPath;
    private string _connectionString;
    private ProveedoresEfRepository _repository;
    private ProductosEfRepository _productosRepository;

    [SetUp]
    public void Setup()
    {
        // Base de datos temporal única por test
        _dbPath = Path.Combine(Path.GetTempPath(), $"cafestock_test_{Guid.NewGuid()}.db");
        _connectionString = $"Data Source={_dbPath}";
        _repository = new ProveedoresEfRepository(_connectionString);
        _productosRepository = new ProductosEfRepository(_connectionString);
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
    public async Task CreateAsync_GuardaProveedor()
    {
        // Arrange
        var proveedor = new Proveedor { Nombre = "Alcampo" };

        // Act
        var resultado = await _repository.CreateAsync(proveedor);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Id.Should().BeGreaterThan(0);
    }

    [Test]
    public async Task DeleteAsync_ProveedorConProductoAsignado_ProductoQuedaSinProveedor()
    {
        // Arrange: producto con proveedor asignado
        var proveedor = await _repository.CreateAsync(new Proveedor { Nombre = "Alcampo" });
        var producto = await _productosRepository.CreateAsync(
            new Producto { Nombre = "Harina", StockActual = 1, StockMaximo = 5, ProveedorId = proveedor.Value.Id });

        // Act: eliminar el proveedor no debe fallar por restricción de FK
        var resultado = await _repository.DeleteAsync(proveedor.Value.Id);

        // Assert
        resultado.IsSuccess.Should().BeTrue();

        var productoActualizado = await _productosRepository.GetByIdAsync(producto.Value.Id);
        productoActualizado.IsSuccess.Should().BeTrue();
        productoActualizado.Value.ProveedorId.Should().BeNull();
    }
}
