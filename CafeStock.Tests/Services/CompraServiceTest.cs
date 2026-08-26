using CafeStock.Back.Errors.Common;
using CafeStock.Back.Errors.Productos;
using CafeStock.Back.Models;
using CafeStock.Back.Repositories.Compras.Base;
using CafeStock.Back.Services.Compras;
using CafeStock.Back.Services.Productos;
using CafeStock.Back.Validators.Compras;
using CSharpFunctionalExtensions;
using FluentAssertions;
using Moq;

namespace CafeStock.Tests.Services;

[TestFixture]
public class CompraServiceTest
{
    private Mock<ICompraRepository> _repositoryMock;
    private Mock<IProductoService> _productoServiceMock;
    private CompraService _service;

    [SetUp]
    public void Setup()
    {
        _repositoryMock = new Mock<ICompraRepository>();
        _productoServiceMock = new Mock<IProductoService>();
        _service = new CompraService(_repositoryMock.Object, new ValidadorCompra(), _productoServiceMock.Object);
    }

    private static Compra CompraConLinea(int productoId, decimal precioUnitario) => new()
    {
        Id = 1,
        ProveedorId = 1,
        Fecha = DateTime.Now,
        Lineas = [new LineaCompra { ProductoId = productoId, Cantidad = 2, PrecioUnitario = precioUnitario }]
    };

    [Test]
    public async Task CreateAsync_PrecioLineaDistintoDelPrecioDeReferencia_ActualizaElPrecioDelProducto()
    {
        // Arrange: el producto tiene 4.00€ de referencia, pero la línea recibida trae 4.50€
        var compra = CompraConLinea(productoId: 1, precioUnitario: 4.5m);
        _repositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<Compra>()))
            .ReturnsAsync(Result.Success<Compra, DomainError>(compra));
        _productoServiceMock
            .Setup(s => s.GetByIdAsync(1))
            .ReturnsAsync(Result.Success<Producto, DomainError>(new Producto { Id = 1, Nombre = "Café", PrecioUnitario = 4.0m }));
        _productoServiceMock
            .Setup(s => s.ActualizarPrecioUnitarioAsync(1, 4.5m))
            .ReturnsAsync(Result.Success<Producto, DomainError>(new Producto { Id = 1, Nombre = "Café", PrecioUnitario = 4.5m }));

        // Act
        var resultado = await _service.CreateAsync(compra);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        _productoServiceMock.Verify(s => s.ActualizarPrecioUnitarioAsync(1, 4.5m), Times.Once);
    }

    [Test]
    public async Task CreateAsync_PrecioLineaIgualAlPrecioDeReferencia_NoActualizaElPrecioDelProducto()
    {
        // Arrange: la línea trae exactamente el mismo precio que ya tiene el producto
        var compra = CompraConLinea(productoId: 1, precioUnitario: 4.0m);
        _repositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<Compra>()))
            .ReturnsAsync(Result.Success<Compra, DomainError>(compra));
        _productoServiceMock
            .Setup(s => s.GetByIdAsync(1))
            .ReturnsAsync(Result.Success<Producto, DomainError>(new Producto { Id = 1, Nombre = "Café", PrecioUnitario = 4.0m }));

        // Act
        var resultado = await _service.CreateAsync(compra);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        _productoServiceMock.Verify(s => s.ActualizarPrecioUnitarioAsync(It.IsAny<int>(), It.IsAny<decimal>()), Times.Never);
    }

    [Test]
    public async Task CreateAsync_NoEncuentraElProducto_NoActualizaYNoTumbaLaCompraYaGuardada()
    {
        // Arrange: la compra se guardó bien, pero el producto ya no existe al sincronizar
        var compra = CompraConLinea(productoId: 99, precioUnitario: 4.5m);
        _repositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<Compra>()))
            .ReturnsAsync(Result.Success<Compra, DomainError>(compra));
        _productoServiceMock
            .Setup(s => s.GetByIdAsync(99))
            .ReturnsAsync(Result.Failure<Producto, DomainError>(ProductoErrors.NotFound(99)));

        // Act
        var resultado = await _service.CreateAsync(compra);

        // Assert: la compra sigue siendo un éxito pese a que la sincronización no pudo hacerse
        resultado.IsSuccess.Should().BeTrue();
        _productoServiceMock.Verify(s => s.ActualizarPrecioUnitarioAsync(It.IsAny<int>(), It.IsAny<decimal>()), Times.Never);
    }

    [Test]
    public async Task CreateAsync_CompraInvalida_NoLlamaAlRepositorioNiSincronizaPrecio()
    {
        // Arrange: sin líneas, la ValidadorCompra real la rechaza antes de llegar al repositorio
        var compra = new Compra { ProveedorId = 1, Fecha = DateTime.Now, Lineas = [] };

        // Act
        var resultado = await _service.CreateAsync(compra);

        // Assert
        resultado.IsFailure.Should().BeTrue();
        _repositoryMock.Verify(r => r.CreateAsync(It.IsAny<Compra>()), Times.Never);
        _productoServiceMock.Verify(s => s.GetByIdAsync(It.IsAny<int>()), Times.Never);
    }

    // ---- ActualizarPrecioLineaAsync ----

    private static Compra CrearCompra(int id, DateTime fecha, int lineaId, int productoId, decimal precioUnitario) => new()
    {
        Id = id,
        Fecha = fecha,
        Lineas = [new LineaCompra { Id = lineaId, CompraId = id, ProductoId = productoId, Cantidad = 2, PrecioUnitario = precioUnitario }]
    };

    [Test]
    public async Task ActualizarPrecioLineaAsync_CompraEsLaMasRecienteDelProducto_ActualizaLaReferencia()
    {
        // Arrange: dos compras del producto 1, la línea 20 es la de la MÁS reciente (día 2)
        var antigua = CrearCompra(id: 1, fecha: new DateTime(2026, 1, 1), lineaId: 10, productoId: 1, precioUnitario: 4.0m);
        var reciente = CrearCompra(id: 2, fecha: new DateTime(2026, 1, 2), lineaId: 20, productoId: 1, precioUnitario: 4.2m);
        _repositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync([antigua, reciente]);
        _repositoryMock
            .Setup(r => r.ActualizarPrecioLineaAsync(20, 5.0m))
            .ReturnsAsync(Result.Success<LineaCompra, DomainError>(reciente.Lineas[0] with { PrecioUnitario = 5.0m }));
        _productoServiceMock
            .Setup(s => s.ActualizarPrecioUnitarioAsync(1, 5.0m))
            .ReturnsAsync(Result.Success<Producto, DomainError>(new Producto { Id = 1, PrecioUnitario = 5.0m }));

        // Act
        var resultado = await _service.ActualizarPrecioLineaAsync(20, 5.0m);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        _productoServiceMock.Verify(s => s.ActualizarPrecioUnitarioAsync(1, 5.0m), Times.Once);
    }

    [Test]
    public async Task ActualizarPrecioLineaAsync_CompraNoEsLaMasRecienteDelProducto_NoTocaLaReferencia()
    {
        // Arrange: dos compras del producto 1; corregimos la línea de la ANTIGUA (día 1),
        // existiendo una posterior (día 2) con el mismo producto
        var antigua = CrearCompra(id: 1, fecha: new DateTime(2026, 1, 1), lineaId: 10, productoId: 1, precioUnitario: 4.0m);
        var reciente = CrearCompra(id: 2, fecha: new DateTime(2026, 1, 2), lineaId: 20, productoId: 1, precioUnitario: 4.2m);
        _repositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync([antigua, reciente]);
        _repositoryMock
            .Setup(r => r.ActualizarPrecioLineaAsync(10, 3.5m))
            .ReturnsAsync(Result.Success<LineaCompra, DomainError>(antigua.Lineas[0] with { PrecioUnitario = 3.5m }));

        // Act
        var resultado = await _service.ActualizarPrecioLineaAsync(10, 3.5m);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
        _productoServiceMock.Verify(s => s.ActualizarPrecioUnitarioAsync(It.IsAny<int>(), It.IsAny<decimal>()), Times.Never);
    }

    [Test]
    public async Task ActualizarPrecioLineaAsync_EmpateDeFecha_GanaElIdDeCompraMasAlto()
    {
        // Arrange: misma Fecha exacta, distinto Id de compra — el desempate es Id desc
        var fechaComun = new DateTime(2026, 1, 1, 10, 0, 0);
        var compraIdBajo = CrearCompra(id: 1, fecha: fechaComun, lineaId: 10, productoId: 1, precioUnitario: 4.0m);
        var compraIdAlto = CrearCompra(id: 2, fecha: fechaComun, lineaId: 20, productoId: 1, precioUnitario: 4.2m);
        _repositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync([compraIdBajo, compraIdAlto]);
        _repositoryMock
            .Setup(r => r.ActualizarPrecioLineaAsync(20, 5.0m))
            .ReturnsAsync(Result.Success<LineaCompra, DomainError>(compraIdAlto.Lineas[0] with { PrecioUnitario = 5.0m }));
        _productoServiceMock
            .Setup(s => s.ActualizarPrecioUnitarioAsync(1, 5.0m))
            .ReturnsAsync(Result.Success<Producto, DomainError>(new Producto { Id = 1, PrecioUnitario = 5.0m }));

        // Act: se corrige la línea de la compra de Id MÁS ALTO (empatada en fecha)
        var resultado = await _service.ActualizarPrecioLineaAsync(20, 5.0m);

        // Assert: gana el desempate, sincroniza la referencia
        resultado.IsSuccess.Should().BeTrue();
        _productoServiceMock.Verify(s => s.ActualizarPrecioUnitarioAsync(1, 5.0m), Times.Once);
    }

    [Test]
    public async Task ActualizarPrecioLineaAsync_EmpateDeFecha_IdMasBajoNoSincroniza()
    {
        // Arrange: mismo empate que el test anterior, pero ahora se corrige la línea del Id MÁS BAJO
        var fechaComun = new DateTime(2026, 1, 1, 10, 0, 0);
        var compraIdBajo = CrearCompra(id: 1, fecha: fechaComun, lineaId: 10, productoId: 1, precioUnitario: 4.0m);
        var compraIdAlto = CrearCompra(id: 2, fecha: fechaComun, lineaId: 20, productoId: 1, precioUnitario: 4.2m);
        _repositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync([compraIdBajo, compraIdAlto]);
        _repositoryMock
            .Setup(r => r.ActualizarPrecioLineaAsync(10, 3.5m))
            .ReturnsAsync(Result.Success<LineaCompra, DomainError>(compraIdBajo.Lineas[0] with { PrecioUnitario = 3.5m }));

        // Act
        var resultado = await _service.ActualizarPrecioLineaAsync(10, 3.5m);

        // Assert: el Id más alto "gana" la recencia, así que el más bajo no sincroniza
        resultado.IsSuccess.Should().BeTrue();
        _productoServiceMock.Verify(s => s.ActualizarPrecioUnitarioAsync(It.IsAny<int>(), It.IsAny<decimal>()), Times.Never);
    }

    [Test]
    public async Task ActualizarPrecioLineaAsync_PrecioCero_NoLlamaAlRepositorio()
    {
        // Act
        var resultado = await _service.ActualizarPrecioLineaAsync(10, 0m);

        // Assert
        resultado.IsFailure.Should().BeTrue();
        _repositoryMock.Verify(r => r.GetAllAsync(), Times.Never);
        _repositoryMock.Verify(r => r.ActualizarPrecioLineaAsync(It.IsAny<int>(), It.IsAny<decimal>()), Times.Never);
    }

    [Test]
    public async Task ActualizarPrecioLineaAsync_LineaNoExiste_DevuelveFailureYNoLlamaANada()
    {
        // Arrange: ninguna compra tiene una línea con Id 999
        var compra = CrearCompra(id: 1, fecha: DateTime.Now, lineaId: 10, productoId: 1, precioUnitario: 4.0m);
        _repositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync([compra]);

        // Act
        var resultado = await _service.ActualizarPrecioLineaAsync(999, 5.0m);

        // Assert
        resultado.IsFailure.Should().BeTrue();
        _repositoryMock.Verify(r => r.ActualizarPrecioLineaAsync(It.IsAny<int>(), It.IsAny<decimal>()), Times.Never);
        _productoServiceMock.Verify(s => s.ActualizarPrecioUnitarioAsync(It.IsAny<int>(), It.IsAny<decimal>()), Times.Never);
    }
}
