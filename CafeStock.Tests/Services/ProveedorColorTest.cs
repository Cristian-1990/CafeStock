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

    // ---------- Texto siempre blanco: el fondo se oscurece solo si hace falta ----------

    [TestCase("#1C7ED6")]   // azul
    [TestCase("#F40009")]   // rojo intenso de Coca-Cola
    [TestCase("#8B4513")]   // marrón de Puchero
    [TestCase("#5C3317")]   // marrón oscuro de Atrian
    [TestCase("#1D3C87")]   // azul oscuro de Délifrance
    public void FondoParaTextoBlanco_SiYaContrasta_NoCambiaElColor(string color)
    {
        PaletaProveedores.FondoParaTextoBlanco(color).Should().Be(color);
    }

    [TestCase("#F59F00")]   // ámbar
    [TestCase("#74B816")]   // lima
    [TestCase("#15AABF")]   // cian
    [TestCase("#FFFFFF")]   // blanco puro
    public void FondoParaTextoBlanco_SiEsMuyClaro_LoOscureceHastaQueSeLeaElBlanco(string color)
    {
        var resultado = PaletaProveedores.FondoParaTextoBlanco(color);

        resultado.Should().NotBe(color);
        PaletaProveedores.ContrasteConBlanco(resultado).Should().BeGreaterThanOrEqualTo(PaletaProveedores.ContrasteMinimoConBlanco);
    }

    [Test]
    public void FondoParaTextoBlanco_ConservaElTono_SoloLoOscurece()
    {
        // El ámbar sigue siendo ámbar (más rojo que verde y más verde que azul), solo más oscuro.
        var (r, g, b) = Canales(PaletaProveedores.FondoParaTextoBlanco("#F59F00"));

        r.Should().BeGreaterThan(g);
        g.Should().BeGreaterThan(b);
        r.Should().BeLessThan(0xF5);
    }

    [Test]
    public void FondoParaTextoBlanco_TodosLosTonosDeLaPaleta_PermitenLeerElBlanco()
    {
        foreach (var color in PaletaProveedores.Colores)
            PaletaProveedores.ContrasteConBlanco(PaletaProveedores.FondoParaTextoBlanco(color))
                .Should().BeGreaterThanOrEqualTo(PaletaProveedores.ContrasteMinimoConBlanco, color);
    }

    [Test]
    public void FondoParaTextoBlanco_ConColorInvalido_DevuelveElMarronNeutro()
    {
        PaletaProveedores.FondoParaTextoBlanco("rojo").Should().Be("#6D4C41");
        PaletaProveedores.FondoParaTextoBlanco(null).Should().Be("#6D4C41");
    }

    private static (int R, int G, int B) Canales(string hex) =>
        (Convert.ToInt32(hex.Substring(1, 2), 16), Convert.ToInt32(hex.Substring(3, 2), 16), Convert.ToInt32(hex.Substring(5, 2), 16));

    [Test]
    public void VariablesCss_ElTextoEsSiempreBlanco()
    {
        PaletaProveedores.VariablesCss(new Proveedor { Id = 1, Color = "#1C7ED6" })
            .Should().Be("--proveedor-color:#1C7ED6;--proveedor-texto:#FFFFFF");

        // Un color claro se oscurece, pero el texto sigue siendo blanco.
        PaletaProveedores.VariablesCss("#F59F00").Should().EndWith("--proveedor-texto:#FFFFFF");
        PaletaProveedores.VariablesCss("#F59F00").Should().NotContain("#F59F00");
    }

    [Test]
    public void VariablesCss_SinProveedorOConColorInvalido_UsaElMarronNeutro()
    {
        PaletaProveedores.VariablesCss((Proveedor?)null).Should().StartWith("--proveedor-color:#6D4C41;");
        PaletaProveedores.VariablesCss("nada").Should().StartWith("--proveedor-color:#6D4C41;");
    }

    [Test]
    public void Paleta_NoIncluyeRojosNiMarrones_ReservadosParaProveedoresDeMarca()
    {
        PaletaProveedores.Colores.Should().NotContain(["#F40009", "#D9534F", "#8B4513"]);
        PaletaProveedores.Colores.Should().OnlyHaveUniqueItems();
        PaletaProveedores.Colores.Should().HaveCount(10);
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
