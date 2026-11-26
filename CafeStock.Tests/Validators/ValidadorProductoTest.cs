using CafeStock.Back.Errors.Productos;
using CafeStock.Back.Models;
using CafeStock.Back.Validators.Productos;
using FluentAssertions;

namespace CafeStock.Tests.Validators;

[TestFixture]
public class ValidadorProductoTest
{
    private ValidadorProducto _validador;

    [SetUp]
    public void Setup()
    {
        _validador = new ValidadorProducto();
    }

    [Test]
    public void Validar_StockConDosDecimales_DevuelveSuccess()
    {
        var producto = new Producto { Nombre = "Harina", StockActual = 0.5m, StockMaximo = 2.25m };

        _validador.Validar(producto).IsSuccess.Should().BeTrue();
    }

    [Test]
    public void Validar_StockActualConMasDeDosDecimales_DevuelveFailure()
    {
        var producto = new Producto { Nombre = "Harina", StockActual = 0.125m, StockMaximo = 2 };

        _validador.Validar(producto).IsFailure.Should().BeTrue();
    }

    [Test]
    public void Validar_StockMaximoConMasDeDosDecimales_DevuelveFailure()
    {
        var producto = new Producto { Nombre = "Harina", StockActual = 1, StockMaximo = 2.001m };

        _validador.Validar(producto).IsFailure.Should().BeTrue();
    }

    [Test]
    public void Validar_ProductoValido_DevuelveSuccess()
    {
        // Arrange
        var producto = new Producto
        {
            Nombre = "Azúcar",
            StockActual = 2,
            StockMaximo = 5
        };

        // Act
        var resultado = _validador.Validar(producto);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
    }

    [Test]
    public void Validar_NombreVacio_DevuelveFailure()
    {
        // Arrange
        var producto = new Producto
        {
            Nombre = "",
            StockActual = 2,
            StockMaximo = 5
        };

        // Act
        var resultado = _validador.Validar(producto);

        // Assert
        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Message.Should().Contain("nombre");
    }

    [Test]
    public void Validar_StockActualNegativo_DevuelveFailure()
    {
        // Arrange
        var producto = new Producto
        {
            Nombre = "Azúcar",
            StockActual = -1,
            StockMaximo = 5
        };

        // Act
        var resultado = _validador.Validar(producto);

        // Assert
        resultado.IsFailure.Should().BeTrue();
    }

    [Test]
    public void Validar_StockMaximoCero_DevuelveFailure()
    {
        // Arrange
        var producto = new Producto
        {
            Nombre = "Azúcar",
            StockActual = 0,
            StockMaximo = 0
        };

        // Act
        var resultado = _validador.Validar(producto);

        // Assert
        resultado.IsFailure.Should().BeTrue();
    }

    [Test]
    public void Validar_StockActualMayorQueMaximo_DevuelveFailure()
    {
        // Arrange
        var producto = new Producto
        {
            Nombre = "Azúcar",
            StockActual = 10,
            StockMaximo = 5
        };

        // Act
        var resultado = _validador.Validar(producto);

        // Assert
        resultado.IsFailure.Should().BeTrue();
    }

    [Test]
    public void Validar_PrecioUnitarioNegativo_DevuelveFailure()
    {
        // Arrange
        var producto = new Producto
        {
            Nombre = "Azúcar",
            StockActual = 2,
            StockMaximo = 5,
            PrecioUnitario = -0.5m
        };

        // Act
        var resultado = _validador.Validar(producto);

        // Assert
        resultado.IsFailure.Should().BeTrue();
    }
}