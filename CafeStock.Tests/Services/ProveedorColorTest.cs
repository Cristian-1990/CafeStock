using CafeStock.Back.Errors.Common;
using CafeStock.Back.Models;
using CafeStock.Back.Repositories.Proveedores.Base;
using CafeStock.Back.Services.Proveedores;
using CafeStock.Back.Validators.Common;
using CSharpFunctionalExtensions;
using FluentAssertions;
using Moq;

namespace CafeStock.Tests.Services;

/// <summary>Color interno de los proveedores: la paleta y su asignación automática al crear.</summary>
[TestFixture]
public class ProveedorColorTest
{
    // ---------- PaletaProveedores ----------

    [Test]
    public void Siguiente_SinColoresUsados_DevuelveElPrimeroDeLaPaleta()
    {
        PaletaProveedores.Siguiente([]).Should().Be(PaletaProveedores.Colores[0]);
    }

    [Test]
    public void Siguiente_IgnoraLosYaUsadosSinImportarMayusculasNiVacios()
    {
        var usados = new[] { PaletaProveedores.Colores[0].ToLowerInvariant(), "", PaletaProveedores.Colores[1] };

        PaletaProveedores.Siguiente(usados).Should().Be(PaletaProveedores.Colores[2]);
    }

    [Test]
    public void Siguiente_ConTodosUsados_VuelveAEmpezar()
    {
        var resultado = PaletaProveedores.Siguiente(PaletaProveedores.Colores);

        PaletaProveedores.Colores.Should().Contain(resultado);
    }

    [TestCase("#A8481F", true)]
    [TestCase("#a8481f", true)]
    [TestCase("A8481F", false)]
    [TestCase("#A8481", false)]
    [TestCase("#GGGGGG", false)]
    [TestCase("", false)]
    [TestCase(null, false)]
    public void EsHexValido_ReconoceElFormato(string? valor, bool esperado)
    {
        PaletaProveedores.EsHexValido(valor).Should().Be(esperado);
    }

    [Test]
    public void ColorDe_ConColorValido_LoDevuelve()
    {
        PaletaProveedores.ColorDe(new Proveedor { Id = 3, Color = "#123456" }).Should().Be("#123456");
    }

    [Test]
    public void ColorDe_SinColor_UsaLaPaletaSegunElId_YEsEstable()
    {
        var proveedor = new Proveedor { Id = 12, Color = "" };

        var primero = PaletaProveedores.ColorDe(proveedor);
        var segundo = PaletaProveedores.ColorDe(proveedor);

        primero.Should().Be(segundo);
        primero.Should().Be(PaletaProveedores.Colores[12 % PaletaProveedores.Colores.Count]);
    }

    [Test]
    public void ColorDe_ConProveedorNulo_DevuelveElMarron()
    {
        PaletaProveedores.ColorDe(null).Should().Be("#6D4C41");
    }

    // ---------- ProveedorService.CreateAsync ----------

    private static (ProveedorService Servicio, Mock<IProveedorRepository> Repo) CrearServicio(IEnumerable<Proveedor> existentes)
    {
        var validador = new Mock<IValidador<Proveedor>>();
        validador.Setup(v => v.Validar(It.IsAny<Proveedor>()))
            .Returns((Proveedor p) => Result.Success<Proveedor, DomainError>(p));

        var repo = new Mock<IProveedorRepository>();
        repo.Setup(r => r.GetAllAsync()).ReturnsAsync(existentes.ToList());
        repo.Setup(r => r.CreateAsync(It.IsAny<Proveedor>()))
            .ReturnsAsync((Proveedor p) => Result.Success<Proveedor, DomainError>(p));

        return (new ProveedorService(repo.Object, validador.Object), repo);
    }

    [Test]
    public async Task CreateAsync_SinColor_AsignaElPrimeroLibreDeLaPaleta()
    {
        var (servicio, repo) = CrearServicio([new Proveedor { Id = 1, Color = PaletaProveedores.Colores[0] }]);

        var resultado = await servicio.CreateAsync(new Proveedor { Nombre = "Nuevo" });

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Color.Should().Be(PaletaProveedores.Colores[1]);
        repo.Verify(r => r.CreateAsync(It.Is<Proveedor>(p => p.Color == PaletaProveedores.Colores[1])), Times.Once);
    }

    [Test]
    public async Task CreateAsync_ConColorValido_LoRespeta()
    {
        var (servicio, _) = CrearServicio([]);

        var resultado = await servicio.CreateAsync(new Proveedor { Nombre = "Nuevo", Color = "#112233" });

        resultado.Value.Color.Should().Be("#112233");
    }

    [Test]
    public async Task CreateAsync_ConColorInvalido_AsignaUnoDeLaPaleta()
    {
        var (servicio, _) = CrearServicio([]);

        var resultado = await servicio.CreateAsync(new Proveedor { Nombre = "Nuevo", Color = "rojo" });

        resultado.Value.Color.Should().Be(PaletaProveedores.Colores[0]);
    }
}
