using CafeStock.Back.Auth;
using CafeStock.Back.Errors.Usuarios;
using CafeStock.Back.Infrastructure;
using CafeStock.Back.Services.Usuarios;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CafeStock.Tests.Services;

/// <summary>
/// Pruebas con Identity real sobre una base SQLite temporal (esquema creado por las migraciones):
/// lo que se comprueba es justo la integración — roles, hashes, validación de contraseñas.
/// </summary>
[TestFixture]
public class UsuarioServiceTest
{
    private string _dbPath = null!;
    private ServiceProvider _provider = null!;
    private IUsuarioService _service = null!;

    [SetUp]
    public async Task Setup()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"cafestock_test_{Guid.NewGuid()}.db");
        var connectionString = $"Data Source={_dbPath}";
        await DatabaseInitializer.EnsureInitializedAsync(connectionString);

        var services = new ServiceCollection();
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.None));
        services.AddCafeStockBack(connectionString);
        services.AddCafeStockIdentity(connectionString);
        _provider = services.BuildServiceProvider();

        _service = _provider.GetRequiredService<IUsuarioService>();
        await _service.AsegurarRolesAsync();
    }

    [TearDown]
    public void TearDown()
    {
        _provider.Dispose();
        SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath))
            File.Delete(_dbPath);
    }

    private async Task<bool> PasswordEsValidaAsync(string nombre, string password)
    {
        using var scope = _provider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var user = await userManager.FindByNameAsync(nombre);
        return user is not null && await userManager.CheckPasswordAsync(user, password);
    }

    // ---------- Roles ----------

    [Test]
    public async Task AsegurarRolesAsync_CreaLosTresRolesYEsIdempotente()
    {
        await _service.AsegurarRolesAsync(); // segunda vez: no debe fallar ni duplicar

        using var scope = _provider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        roleManager.Roles.Select(r => r.Name).ToList().Should().BeEquivalentTo(Roles.Todos);
    }

    [TestCase(Politicas.Trabajador, new[] { "Trabajador", "Encargado", "Admin" })]
    [TestCase(Politicas.Encargado, new[] { "Encargado", "Admin" })]
    [TestCase(Politicas.Admin, new[] { "Admin" })]
    public void Politicas_IncluyenLosRolesSuperiores(string politica, string[] esperados)
    {
        Politicas.RolesPermitidos(politica).Should().BeEquivalentTo(esperados);
    }

    // ---------- Crear ----------

    [Test]
    public async Task CrearAsync_DatosValidos_CreaElUsuarioConSuRol()
    {
        var resultado = await _service.CrearAsync("  ana  ", "cafe1234", Roles.Encargado);

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.NombreUsuario.Should().Be("ana"); // recortado
        resultado.Value.Rol.Should().Be(Roles.Encargado);
        (await _service.ListarAsync()).Should().ContainSingle().Which.NombreUsuario.Should().Be("ana");
        (await PasswordEsValidaAsync("ana", "cafe1234")).Should().BeTrue();
    }

    [Test]
    public async Task CrearAsync_RolInexistente_DevuelveFailure()
    {
        var resultado = await _service.CrearAsync("ana", "cafe1234", "Superman");

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().BeOfType<UsuarioError.RolInvalido>();
        (await _service.ListarAsync()).Should().BeEmpty();
    }

    [TestCase("corta1", "al menos 8")]
    [TestCase("sinnumeros", "un número")]
    [TestCase("12345678", "una minúscula")]
    public async Task CrearAsync_ContrasenaDebil_FallaConMensajeEnEspanolYNoCreaNada(string password, string fragmentoMensaje)
    {
        var resultado = await _service.CrearAsync("ana", password, Roles.Trabajador);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Message.Should().Contain(fragmentoMensaje);
        (await _service.ListarAsync()).Should().BeEmpty();
    }

    [Test]
    public async Task CrearAsync_NombreDuplicado_DevuelveFailure()
    {
        await _service.CrearAsync("ana", "cafe1234", Roles.Trabajador);

        var resultado = await _service.CrearAsync("ANA", "cafe1234", Roles.Trabajador); // sin distinguir mayúsculas

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Message.Should().Contain("ya existe");
        (await _service.ListarAsync()).Should().HaveCount(1);
    }

    // ---------- Primer administrador ----------

    [Test]
    public async Task CrearPrimerAdminAsync_SinUsuarios_CreaUnAdmin()
    {
        (await _service.HayUsuariosAsync()).Should().BeFalse();

        var resultado = await _service.CrearPrimerAdminAsync("jefe", "cafe1234");

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.Rol.Should().Be(Roles.Admin);
        (await _service.HayUsuariosAsync()).Should().BeTrue();
    }

    [Test]
    public async Task CrearPrimerAdminAsync_YaHayUsuarios_NoPermiteCrearOtroAdmin()
    {
        await _service.CrearAsync("ana", "cafe1234", Roles.Trabajador);

        var resultado = await _service.CrearPrimerAdminAsync("intruso", "cafe1234");

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().BeOfType<UsuarioError.YaHayUsuarios>();
        (await _service.ListarAsync()).Should().NotContain(u => u.NombreUsuario == "intruso");
    }

    // ---------- Cambiar rol ----------

    [Test]
    public async Task CambiarRolAsync_CambiaElRol()
    {
        var ana = (await _service.CrearAsync("ana", "cafe1234", Roles.Trabajador)).Value;

        var resultado = await _service.CambiarRolAsync(ana.Id, Roles.Encargado);

        resultado.IsSuccess.Should().BeTrue();
        (await _service.ListarAsync()).Single().Rol.Should().Be(Roles.Encargado);
    }

    [Test]
    public async Task CambiarRolAsync_UnicoAdmin_NoPermiteQuitarleElRol()
    {
        var jefe = (await _service.CrearAsync("jefe", "cafe1234", Roles.Admin)).Value;

        var resultado = await _service.CambiarRolAsync(jefe.Id, Roles.Trabajador);

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().BeOfType<UsuarioError.UltimoAdmin>();
        (await _service.ListarAsync()).Single().Rol.Should().Be(Roles.Admin);
    }

    [Test]
    public async Task CambiarRolAsync_ConOtroAdminDisponible_PermiteQuitarleElRol()
    {
        var jefe = (await _service.CrearAsync("jefe", "cafe1234", Roles.Admin)).Value;
        await _service.CrearAsync("jefa", "cafe1234", Roles.Admin);

        var resultado = await _service.CambiarRolAsync(jefe.Id, Roles.Encargado);

        resultado.IsSuccess.Should().BeTrue();
    }

    [Test]
    public async Task CambiarRolAsync_UsuarioInexistente_DevuelveFailure()
    {
        var resultado = await _service.CambiarRolAsync("no-existe", Roles.Encargado);

        resultado.Error.Should().BeOfType<UsuarioError.NotFound>();
    }

    // ---------- Restablecer contraseña ----------

    [Test]
    public async Task RestablecerPasswordAsync_ContrasenaValida_SustituyeLaAnterior()
    {
        var ana = (await _service.CrearAsync("ana", "cafe1234", Roles.Trabajador)).Value;

        var resultado = await _service.RestablecerPasswordAsync(ana.Id, "nueva5678");

        resultado.IsSuccess.Should().BeTrue();
        (await PasswordEsValidaAsync("ana", "nueva5678")).Should().BeTrue();
        (await PasswordEsValidaAsync("ana", "cafe1234")).Should().BeFalse();
    }

    [Test]
    public async Task RestablecerPasswordAsync_ContrasenaDebil_FallaYConservaLaAnterior()
    {
        var ana = (await _service.CrearAsync("ana", "cafe1234", Roles.Trabajador)).Value;

        var resultado = await _service.RestablecerPasswordAsync(ana.Id, "corta");

        // Regresión pensada: no debe dejar al usuario sin contraseña si la nueva no vale.
        resultado.IsFailure.Should().BeTrue();
        (await PasswordEsValidaAsync("ana", "cafe1234")).Should().BeTrue();
    }

    // ---------- Renombrar ----------

    [Test]
    public async Task RenombrarAsync_NombreNuevo_CambiaElNombreYConservaRolYContrasena()
    {
        var ana = (await _service.CrearAsync("ana", "cafe1234", Roles.Encargado)).Value;

        var resultado = await _service.RenombrarAsync(ana.Id, "  anabel ");

        resultado.IsSuccess.Should().BeTrue();
        resultado.Value.NombreUsuario.Should().Be("anabel");
        resultado.Value.Rol.Should().Be(Roles.Encargado);
        (await PasswordEsValidaAsync("anabel", "cafe1234")).Should().BeTrue();
        (await PasswordEsValidaAsync("ana", "cafe1234")).Should().BeFalse();
    }

    [Test]
    public async Task RenombrarAsync_NombreYaUsado_FallaYConservaElNombre()
    {
        var ana = (await _service.CrearAsync("ana", "cafe1234", Roles.Trabajador)).Value;
        await _service.CrearAsync("luis", "cafe1234", Roles.Trabajador);

        var resultado = await _service.RenombrarAsync(ana.Id, "luis");

        resultado.IsFailure.Should().BeTrue();
        (await PasswordEsValidaAsync("ana", "cafe1234")).Should().BeTrue();
    }

    [Test]
    public async Task RenombrarAsync_NombreVacio_Falla()
    {
        var ana = (await _service.CrearAsync("ana", "cafe1234", Roles.Trabajador)).Value;

        var resultado = await _service.RenombrarAsync(ana.Id, "   ");

        resultado.IsFailure.Should().BeTrue();
        (await PasswordEsValidaAsync("ana", "cafe1234")).Should().BeTrue();
    }

    [Test]
    public async Task RenombrarAsync_UsuarioInexistente_FallaConNotFound()
    {
        var resultado = await _service.RenombrarAsync("no-existe", "pepe");

        resultado.IsFailure.Should().BeTrue();
        resultado.Error.Should().BeOfType<UsuarioError.NotFound>();
    }

    // ---------- Eliminar ----------

    [Test]
    public async Task EliminarAsync_OtroUsuario_LoElimina()
    {
        var jefe = (await _service.CrearAsync("jefe", "cafe1234", Roles.Admin)).Value;
        var ana = (await _service.CrearAsync("ana", "cafe1234", Roles.Trabajador)).Value;

        var resultado = await _service.EliminarAsync(ana.Id, jefe.Id);

        resultado.IsSuccess.Should().BeTrue();
        (await _service.ListarAsync()).Should().ContainSingle().Which.NombreUsuario.Should().Be("jefe");
    }

    [Test]
    public async Task EliminarAsync_UnoMismo_DevuelveFailure()
    {
        var jefe = (await _service.CrearAsync("jefe", "cafe1234", Roles.Admin)).Value;

        var resultado = await _service.EliminarAsync(jefe.Id, jefe.Id);

        resultado.Error.Should().BeOfType<UsuarioError.AutoEliminacion>();
    }

    [Test]
    public async Task EliminarAsync_UnicoAdmin_NoPermiteEliminarloAunqueLoPidaOtro()
    {
        var jefe = (await _service.CrearAsync("jefe", "cafe1234", Roles.Admin)).Value;
        var ana = (await _service.CrearAsync("ana", "cafe1234", Roles.Encargado)).Value;

        var resultado = await _service.EliminarAsync(jefe.Id, ana.Id);

        resultado.Error.Should().BeOfType<UsuarioError.UltimoAdmin>();
        (await _service.ListarAsync()).Should().HaveCount(2);
    }

    [Test]
    public async Task EliminarAsync_UsuarioInexistente_DevuelveFailure()
    {
        var resultado = await _service.EliminarAsync("no-existe", "otro");

        resultado.Error.Should().BeOfType<UsuarioError.NotFound>();
    }
}
