using CafeStock.Back.Errors.Common;
using CafeStock.Back.Errors.SnapshotsInventario;
using CafeStock.Back.Models;
using CafeStock.Back.Repositories.SnapshotsInventario.Base;
using CafeStock.Back.Services.Productos;
using CafeStock.Back.Services.SnapshotsInventario;
using CSharpFunctionalExtensions;
using FluentAssertions;
using Moq;

namespace CafeStock.Tests.Services;

[TestFixture]
public class SnapshotInventarioServiceTest
{
    private Mock<ISnapshotInventarioRepository> _repositoryMock;
    private Mock<IProductoService> _productoServiceMock;
    private SnapshotInventarioService _service;

    [SetUp]
    public void Setup()
    {
        _repositoryMock = new Mock<ISnapshotInventarioRepository>();
        _productoServiceMock = new Mock<IProductoService>();
        _service = new SnapshotInventarioService(_repositoryMock.Object, _productoServiceMock.Object);
    }

    [Test]
    public async Task AsegurarSnapshotDeHoyAsync_YaExisteSnapshotDeHoy_NoLlamaACreateAsync()
    {
        // Arrange
        var hoy = DateOnly.FromDateTime(DateTime.Now);
        _repositoryMock
            .Setup(r => r.GetByFechaAsync(hoy))
            .ReturnsAsync(Result.Success<SnapshotInventario, DomainError>(new SnapshotInventario { Fecha = hoy, ValorTotal = 100m }));

        // Act
        await _service.AsegurarSnapshotDeHoyAsync();

        // Assert
        _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<SnapshotInventario>()), Times.Never);
        _productoServiceMock.Verify(p => p.GetAllAsync(), Times.Never);
    }

    [Test]
    public async Task AsegurarSnapshotDeHoyAsync_NoExisteTodavia_CalculaYCreaConLaSumaCorrecta()
    {
        // Arrange
        var hoy = DateOnly.FromDateTime(DateTime.Now);
        _repositoryMock
            .Setup(r => r.GetByFechaAsync(hoy))
            .ReturnsAsync(Result.Failure<SnapshotInventario, DomainError>(SnapshotInventarioErrors.NotFound(hoy)));

        var productos = new List<Producto>
        {
            new() { Nombre = "Café", StockActual = 10, StockMaximo = 20, PrecioUnitario = 3m },
            new() { Nombre = "Azúcar", StockActual = 5, StockMaximo = 10, PrecioUnitario = 1.5m }
        };
        _productoServiceMock.Setup(p => p.GetAllAsync()).ReturnsAsync(productos);

        SnapshotInventario? snapshotCreado = null;
        _repositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<SnapshotInventario>()))
            .Callback<SnapshotInventario>(s => snapshotCreado = s)
            .ReturnsAsync((SnapshotInventario s) => Result.Success<SnapshotInventario, DomainError>(s));

        // Act
        await _service.AsegurarSnapshotDeHoyAsync();

        // Assert: 10*3 + 5*1.5 = 37.5
        _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<SnapshotInventario>()), Times.Once);
        snapshotCreado.Should().NotBeNull();
        snapshotCreado!.Fecha.Should().Be(hoy);
        snapshotCreado.ValorTotal.Should().Be(37.5m);
    }

    [Test]
    public async Task AsegurarSnapshotDeHoyAsync_FallaAlCrear_NoLanzaExcepcion()
    {
        // Arrange
        var hoy = DateOnly.FromDateTime(DateTime.Now);
        _repositoryMock
            .Setup(r => r.GetByFechaAsync(hoy))
            .ReturnsAsync(Result.Failure<SnapshotInventario, DomainError>(SnapshotInventarioErrors.NotFound(hoy)));
        _productoServiceMock.Setup(p => p.GetAllAsync()).ReturnsAsync([]);
        _repositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<SnapshotInventario>()))
            .ReturnsAsync(Result.Failure<SnapshotInventario, DomainError>(SnapshotInventarioErrors.DatabaseError("boom")));

        // Act
        var accion = async () => await _service.AsegurarSnapshotDeHoyAsync();

        // Assert: es una operación de "asegurar estado" en segundo plano, no debe poder
        // romper la carga de Informes si falla.
        await accion.Should().NotThrowAsync();
    }
}
