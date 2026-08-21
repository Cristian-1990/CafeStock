using CafeStock.Back.Models;
using CafeStock.Back.Validators.Presupuestos;
using FluentAssertions;

namespace CafeStock.Tests.Validators;

[TestFixture]
public class ValidadorPresupuestoTest
{
    private ValidadorPresupuesto _validador;

    [SetUp]
    public void Setup()
    {
        _validador = new ValidadorPresupuesto();
    }

    [Test]
    public void Validar_PresupuestoValido_DevuelveSuccess()
    {
        // Arrange
        var presupuesto = new Presupuesto { ProveedorId = 1, Mes = 6, Anio = 2025, ImporteAsignado = 500m };

        // Act
        var resultado = _validador.Validar(presupuesto);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
    }

    [Test]
    public void Validar_ImporteAsignadoCero_DevuelveFailure()
    {
        // Arrange
        var presupuesto = new Presupuesto { ProveedorId = 1, Mes = 6, Anio = 2025, ImporteAsignado = 0m };

        // Act
        var resultado = _validador.Validar(presupuesto);

        // Assert
        resultado.IsFailure.Should().BeTrue();
    }

    [Test]
    public void Validar_ImporteAsignadoNegativo_DevuelveFailure()
    {
        // Arrange
        var presupuesto = new Presupuesto { ProveedorId = 1, Mes = 6, Anio = 2025, ImporteAsignado = -100m };

        // Act
        var resultado = _validador.Validar(presupuesto);

        // Assert
        resultado.IsFailure.Should().BeTrue();
    }

    [Test]
    public void Validar_MesCero_DevuelveFailure()
    {
        // Arrange
        var presupuesto = new Presupuesto { ProveedorId = 1, Mes = 0, Anio = 2025, ImporteAsignado = 500m };

        // Act
        var resultado = _validador.Validar(presupuesto);

        // Assert
        resultado.IsFailure.Should().BeTrue();
    }

    [Test]
    public void Validar_MesTrece_DevuelveFailure()
    {
        // Arrange
        var presupuesto = new Presupuesto { ProveedorId = 1, Mes = 13, Anio = 2025, ImporteAsignado = 500m };

        // Act
        var resultado = _validador.Validar(presupuesto);

        // Assert
        resultado.IsFailure.Should().BeTrue();
    }

    [Test]
    public void Validar_AnioAnteriorA2024_DevuelveFailure()
    {
        // Arrange
        var presupuesto = new Presupuesto { ProveedorId = 1, Mes = 6, Anio = 2023, ImporteAsignado = 500m };

        // Act
        var resultado = _validador.Validar(presupuesto);

        // Assert
        resultado.IsFailure.Should().BeTrue();
    }

    [Test]
    public void Validar_Anio2024_DevuelveSuccess()
    {
        // Arrange
        var presupuesto = new Presupuesto { ProveedorId = 1, Mes = 1, Anio = 2024, ImporteAsignado = 500m };

        // Act
        var resultado = _validador.Validar(presupuesto);

        // Assert
        resultado.IsSuccess.Should().BeTrue();
    }
}
