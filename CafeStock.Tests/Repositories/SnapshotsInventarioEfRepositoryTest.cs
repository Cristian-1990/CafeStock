using CafeStock.Back.Models;
using CafeStock.Back.Repositories.SnapshotsInventario.EfCore;
using FluentAssertions;

namespace CafeStock.Tests.Repositories;

[TestFixture]
public class SnapshotsInventarioEfRepositoryTest
{
    private string _dbPath;
    private string _connectionString;
    private SnapshotsInventarioEfRepository _repository;

    [SetUp]
    public void Setup()
    {
        // Base de datos temporal única por test
        _dbPath = Path.Combine(Path.GetTempPath(), $"cafestock_test_{Guid.NewGuid()}.db");
        _connectionString = $"Data Source={_dbPath}";
        _repository = new SnapshotsInventarioEfRepository(_connectionString);
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
    public async Task CreateAsync_SnapshotValido_GuardaYDevuelveConId()
    {
        // Arrange
        var snapshot = new SnapshotInventario { Fecha = new DateOnly(2025, 6, 15), ValorTotal = 1234.56m };

        // Act
        var resultado = await _repository.CreateAsync(snapshot);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Id.Should().BeGreaterThan(0);
        resultado.Value.ValorTotal.Should().Be(1234.56m);
    }

    [Test]
    public async Task CreateAsync_MismaFechaDosVeces_LaSegundaDevuelveFailure()
    {
        // Arrange: el índice único de Fecha impide dos snapshots el mismo día.
        var fecha = new DateOnly(2025, 6, 15);
        await _repository.CreateAsync(new SnapshotInventario { Fecha = fecha, ValorTotal = 100m });

        // Act
        var resultado = await _repository.CreateAsync(new SnapshotInventario { Fecha = fecha, ValorTotal = 200m });

        // Assert
        resultado.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task GetByFechaAsync_NoExiste_DevuelveFailure()
    {
        // Act
        var resultado = await _repository.GetByFechaAsync(new DateOnly(2025, 6, 15));

        // Assert
        resultado.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task GetByFechaAsync_Existe_DevuelveElSnapshot()
    {
        // Arrange
        var fecha = new DateOnly(2025, 6, 15);
        await _repository.CreateAsync(new SnapshotInventario { Fecha = fecha, ValorTotal = 1234.56m });

        // Act
        var resultado = await _repository.GetByFechaAsync(fecha);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.ValorTotal.Should().Be(1234.56m);
    }

    [Test]
    public async Task GetAllAsync_DevuelveTodosLosSnapshots()
    {
        // Arrange
        await _repository.CreateAsync(new SnapshotInventario { Fecha = new DateOnly(2025, 6, 15), ValorTotal = 1000m });
        await _repository.CreateAsync(new SnapshotInventario { Fecha = new DateOnly(2025, 6, 16), ValorTotal = 1100m });

        // Act
        var resultado = await _repository.GetAllAsync();

        // Assert
        resultado.Should().HaveCount(2);
    }

    [Test]
    public async Task GetAllAsync_SinSnapshots_DevuelveVacio()
    {
        // Act
        var resultado = await _repository.GetAllAsync();

        // Assert
        resultado.Should().BeEmpty();
    }
}
