using CafeStock.Back.Entity;
using CafeStock.Back.Models;
using CafeStock.Back.Repositories.Productos.EfCore;
using CafeStock.Back.Repositories.Proveedores.EfCore;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

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
    public async Task UpdateAsync_ModificaAgruparPorTipoUnidad()
    {
        // Arrange
        var creado = await _repository.CreateAsync(new Proveedor { Nombre = "Alcampo", AgruparPorTipoUnidad = false });
        var modificado = creado.Value with { AgruparPorTipoUnidad = true };

        // Act
        var resultado = await _repository.UpdateAsync(creado.Value.Id, modificado);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.AgruparPorTipoUnidad.Should().BeTrue();
    }

    [Test]
    public async Task Initialize_ProveedorPuchero_ActivaAgruparPorTipoUnidad()
    {
        // Arrange: Puchero se crea como cualquier otro proveedor, sin el flag
        var pucheroSeed = new ProveedoresEfRepository(_connectionString);
        await pucheroSeed.CreateAsync(new Proveedor { Nombre = "Puchero" });

        // Act: _repository (fresca, _initialized aún en false) dispara la migración en su
        // primera llamada — como al arrancar la app de verdad.
        var proveedores = (await _repository.GetAllAsync()).ToList();

        // Assert
        var puchero = proveedores.Single(p => p.Nombre == "Puchero");
        puchero.AgruparPorTipoUnidad.Should().BeTrue();
    }

    [Test]
    public async Task Initialize_ProveedorSinNombrePuchero_NoActivaAgruparPorTipoUnidad()
    {
        // Arrange: un proveedor cualquiera, distinto de Puchero
        var alcampoSeed = new ProveedoresEfRepository(_connectionString);
        await alcampoSeed.CreateAsync(new Proveedor { Nombre = "Alcampo" });

        // Act
        var proveedores = (await _repository.GetAllAsync()).ToList();

        // Assert
        proveedores.Single().AgruparPorTipoUnidad.Should().BeFalse();
    }

    [Test]
    public async Task UpdateAsync_ModificaPrecioUnitarioAltaPrecision()
    {
        // Arrange
        var creado = await _repository.CreateAsync(new Proveedor { Nombre = "Alcampo", PrecioUnitarioAltaPrecision = false });
        var modificado = creado.Value with { PrecioUnitarioAltaPrecision = true };

        // Act
        var resultado = await _repository.UpdateAsync(creado.Value.Id, modificado);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.PrecioUnitarioAltaPrecision.Should().BeTrue();
    }

    [Test]
    public async Task Initialize_ProveedorPriegola_ActivaPrecioUnitarioAltaPrecision()
    {
        // Arrange: Priegola se crea como cualquier otro proveedor, sin el flag
        var priegolaSeed = new ProveedoresEfRepository(_connectionString);
        await priegolaSeed.CreateAsync(new Proveedor { Nombre = "Priegola" });

        // Act: _repository (fresca, _initialized aún en false) dispara la migración en su
        // primera llamada — como al arrancar la app de verdad.
        var proveedores = (await _repository.GetAllAsync()).ToList();

        // Assert
        var priegola = proveedores.Single(p => p.Nombre == "Priegola");
        priegola.PrecioUnitarioAltaPrecision.Should().BeTrue();
    }

    [Test]
    public async Task Initialize_ProveedorSinNombrePriegola_NoActivaPrecioUnitarioAltaPrecision()
    {
        // Arrange: un proveedor cualquiera, distinto de Priegola
        var alcampoSeed = new ProveedoresEfRepository(_connectionString);
        await alcampoSeed.CreateAsync(new Proveedor { Nombre = "Alcampo" });

        // Act
        var proveedores = (await _repository.GetAllAsync()).ToList();

        // Assert
        proveedores.Single().PrecioUnitarioAltaPrecision.Should().BeFalse();
    }

    [Test]
    public async Task Initialize_BaseDatosSinColumnaPrecioAltaPrecisionTodavia_NoFallaAlConsultarProveedores()
    {
        // Arrange: simula una base de datos real creada ANTES de que existiera la columna
        // PrecioUnitarioAltaPrecision — regresión de un bug real de producción donde
        // AsegurarAgrupacionPorUnidadPucheroAsync (una consulta LINQ normal, no SQL crudo) se
        // ejecutaba antes de que AsegurarColumnaPrecioUnitarioAltaPrecisionAsync añadiera la
        // columna. EF Core selecciona TODAS las columnas mapeadas del modelo en cualquier
        // consulta LINQ, así que cualquier método que consulte Proveedores por LINQ revienta
        // si se cuela antes de que todas las columnas nuevas existan — no solo el que
        // introdujo el campo.
        await using (var seedContext = new AppDbContext(_connectionString))
        {
            await seedContext.EnsureCreatedAsync();
            var conexion = seedContext.Database.GetDbConnection();
            await conexion.OpenAsync();
            await using var comando = conexion.CreateCommand();
            comando.CommandText = "ALTER TABLE Proveedores DROP COLUMN PrecioUnitarioAltaPrecision";
            await comando.ExecuteNonQueryAsync();
        }

        // Act: _repository (fresca, _initialized aún en false) dispara toda la inicialización
        // en su primera llamada — como al arrancar la app de verdad contra un .db existente.
        var accion = async () => await _repository.GetAllAsync();

        // Assert
        await accion.Should().NotThrowAsync();
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
