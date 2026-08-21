using CafeStock.Back.Models;
using CafeStock.Back.Repositories.Presupuestos.EfCore;
using CafeStock.Back.Repositories.Proveedores.EfCore;
using FluentAssertions;

namespace CafeStock.Tests.Repositories;

[TestFixture]
public class PresupuestosEfRepositoryTest
{
    private string _dbPath;
    private string _connectionString;
    private PresupuestosEfRepository _repository;
    private ProveedoresEfRepository _proveedorRepository;

    [SetUp]
    public void Setup()
    {
        // Base de datos temporal única por test
        _dbPath = Path.Combine(Path.GetTempPath(), $"cafestock_test_{Guid.NewGuid()}.db");
        _connectionString = $"Data Source={_dbPath}";
        _repository = new PresupuestosEfRepository(_connectionString);
        _proveedorRepository = new ProveedoresEfRepository(_connectionString);
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
    public async Task CreateAsync_PresupuestoValido_GuardaYDevuelveConId()
    {
        // Arrange
        var proveedor = await _proveedorRepository.CreateAsync(new Proveedor { Nombre = "Alcampo" });
        var presupuesto = new Presupuesto { ProveedorId = proveedor.Value.Id, Mes = 6, Anio = 2025, ImporteAsignado = 500m };

        // Act
        var resultado = await _repository.CreateAsync(presupuesto);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Id.Should().BeGreaterThan(0);
        resultado.Value.ImporteAsignado.Should().Be(500m);
    }

    [Test]
    public async Task CreateAsync_ProveedorNoExiste_DevuelveFailure()
    {
        // Arrange
        var presupuesto = new Presupuesto { ProveedorId = 999, Mes = 6, Anio = 2025, ImporteAsignado = 500m };

        // Act
        var resultado = await _repository.CreateAsync(presupuesto);

        // Assert
        resultado.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task CreateAsync_MismoProveedorMesYAnio_DevuelveFailure()
    {
        // Arrange: el índice único (y la comprobación previa del repositorio) impiden dos
        // presupuestos para el mismo proveedor+mes+año.
        var proveedor = await _proveedorRepository.CreateAsync(new Proveedor { Nombre = "Alcampo" });
        await _repository.CreateAsync(new Presupuesto { ProveedorId = proveedor.Value.Id, Mes = 6, Anio = 2025, ImporteAsignado = 500m });

        // Act
        var resultado = await _repository.CreateAsync(
            new Presupuesto { ProveedorId = proveedor.Value.Id, Mes = 6, Anio = 2025, ImporteAsignado = 300m });

        // Assert
        resultado.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task CreateAsync_MismoProveedorDistintoMes_PermiteAmbos()
    {
        // Arrange
        var proveedor = await _proveedorRepository.CreateAsync(new Proveedor { Nombre = "Alcampo" });
        await _repository.CreateAsync(new Presupuesto { ProveedorId = proveedor.Value.Id, Mes = 6, Anio = 2025, ImporteAsignado = 500m });

        // Act
        var resultado = await _repository.CreateAsync(
            new Presupuesto { ProveedorId = proveedor.Value.Id, Mes = 7, Anio = 2025, ImporteAsignado = 600m });

        // Assert
        resultado.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task GetByProveedorMesAnioAsync_NoExiste_DevuelveFailure()
    {
        // Arrange
        var proveedor = await _proveedorRepository.CreateAsync(new Proveedor { Nombre = "Alcampo" });

        // Act
        var resultado = await _repository.GetByProveedorMesAnioAsync(proveedor.Value.Id, 6, 2025);

        // Assert
        resultado.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task GetByProveedorMesAnioAsync_Existe_DevuelveElPresupuesto()
    {
        // Arrange
        var proveedor = await _proveedorRepository.CreateAsync(new Proveedor { Nombre = "Alcampo" });
        await _repository.CreateAsync(new Presupuesto { ProveedorId = proveedor.Value.Id, Mes = 6, Anio = 2025, ImporteAsignado = 500m });

        // Act
        var resultado = await _repository.GetByProveedorMesAnioAsync(proveedor.Value.Id, 6, 2025);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.ImporteAsignado.Should().Be(500m);
    }

    [Test]
    public async Task ActualizarImporteAsync_PresupuestoExiste_ActualizaSoloElImporte()
    {
        // Arrange
        var proveedor = await _proveedorRepository.CreateAsync(new Proveedor { Nombre = "Alcampo" });
        var creado = await _repository.CreateAsync(
            new Presupuesto { ProveedorId = proveedor.Value.Id, Mes = 6, Anio = 2025, ImporteAsignado = 500m });

        // Act
        var resultado = await _repository.ActualizarImporteAsync(creado.Value.Id, 750m);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.ImporteAsignado.Should().Be(750m);
        resultado.Value.Mes.Should().Be(6);
        resultado.Value.Anio.Should().Be(2025);
    }

    [Test]
    public async Task GetByProveedorAsync_DevuelveSoloLosDeEseProveedor()
    {
        // Arrange
        var proveedor1 = await _proveedorRepository.CreateAsync(new Proveedor { Nombre = "Alcampo" });
        var proveedor2 = await _proveedorRepository.CreateAsync(new Proveedor { Nombre = "Puchero" });
        await _repository.CreateAsync(new Presupuesto { ProveedorId = proveedor1.Value.Id, Mes = 6, Anio = 2025, ImporteAsignado = 500m });
        await _repository.CreateAsync(new Presupuesto { ProveedorId = proveedor2.Value.Id, Mes = 6, Anio = 2025, ImporteAsignado = 300m });

        // Act
        var resultado = await _repository.GetByProveedorAsync(proveedor1.Value.Id);

        // Assert
        resultado.Should().ContainSingle();
        resultado.Single().ImporteAsignado.Should().Be(500m);
    }

    [Test]
    public async Task GetAllAsync_DevuelveLosDeTodosLosProveedores()
    {
        // Arrange
        var proveedor1 = await _proveedorRepository.CreateAsync(new Proveedor { Nombre = "Alcampo" });
        var proveedor2 = await _proveedorRepository.CreateAsync(new Proveedor { Nombre = "Puchero" });
        await _repository.CreateAsync(new Presupuesto { ProveedorId = proveedor1.Value.Id, Mes = 6, Anio = 2025, ImporteAsignado = 500m });
        await _repository.CreateAsync(new Presupuesto { ProveedorId = proveedor2.Value.Id, Mes = 7, Anio = 2025, ImporteAsignado = 300m });

        // Act
        var resultado = await _repository.GetAllAsync();

        // Assert
        resultado.Should().HaveCount(2);
    }

    [Test]
    public async Task DeleteAsync_PresupuestoExiste_LoElimina()
    {
        // Arrange
        var proveedor = await _proveedorRepository.CreateAsync(new Proveedor { Nombre = "Alcampo" });
        var creado = await _repository.CreateAsync(
            new Presupuesto { ProveedorId = proveedor.Value.Id, Mes = 6, Anio = 2025, ImporteAsignado = 500m });

        // Act
        var resultado = await _repository.DeleteAsync(creado.Value.Id);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        (await _repository.GetByProveedorAsync(proveedor.Value.Id)).Should().BeEmpty();
    }

    [Test]
    public async Task DeleteAsync_NoExiste_DevuelveFailure()
    {
        // Act
        var resultado = await _repository.DeleteAsync(999);

        // Assert
        resultado.IsFailure.Should().BeTrue();
    }

    [Test]
    public async Task DeleteAsync_PermiteCrearOtroConElMismoMesYAnioTrasBorrar()
    {
        // Arrange: la vía de corrección para un Mes/Año equivocado es borrar y crear de nuevo
        // (Mes/Año no se pueden editar) — comprueba que el índice único no bloquea el segundo
        // Create una vez borrado el primero.
        var proveedor = await _proveedorRepository.CreateAsync(new Proveedor { Nombre = "Alcampo" });
        var creado = await _repository.CreateAsync(
            new Presupuesto { ProveedorId = proveedor.Value.Id, Mes = 6, Anio = 2025, ImporteAsignado = 500m });
        await _repository.DeleteAsync(creado.Value.Id);

        // Act
        var resultado = await _repository.CreateAsync(
            new Presupuesto { ProveedorId = proveedor.Value.Id, Mes = 6, Anio = 2025, ImporteAsignado = 700m });

        // Assert
        resultado.IsSuccess.Should().BeTrue();
    }
}
