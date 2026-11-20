namespace CafeStock.Back.Auth;

/// <summary>
/// Roles de la aplicación. Son jerárquicos en la práctica (el Admin puede todo lo del Encargado
/// y el Encargado todo lo del Trabajador), pero Identity no tiene jerarquía propia: se resuelve
/// con las políticas de <see cref="Politicas"/>, no preguntando por un rol concreto en cada página.
/// </summary>
public static class Roles
{
    public const string Trabajador = "Trabajador";
    public const string Encargado = "Encargado";
    public const string Admin = "Admin";

    public static readonly IReadOnlyList<string> Todos = [Trabajador, Encargado, Admin];

    public static bool EsValido(string? rol) => rol is not null && Todos.Contains(rol);
}

/// <summary>
/// Nombres de las políticas de autorización (se registran en Program.cs). Cada una incluye los
/// roles superiores: p. ej. una página de "Trabajador" también la ve un Encargado o un Admin.
/// </summary>
public static class Politicas
{
    /// <summary>Stock, Venta, Recepción: cualquier usuario con sesión iniciada.</summary>
    public const string Trabajador = "Trabajador";

    /// <summary>Productos, Proveedores, Facturas, Informes: Encargado y Admin.</summary>
    public const string Encargado = "Encargado";

    /// <summary>Gestión de usuarios: solo Admin.</summary>
    public const string Admin = "Admin";

    /// <summary>Roles que satisfacen cada política.</summary>
    public static IReadOnlyList<string> RolesPermitidos(string politica) => politica switch
    {
        Trabajador => [Roles.Trabajador, Roles.Encargado, Roles.Admin],
        Encargado => [Roles.Encargado, Roles.Admin],
        Admin => [Roles.Admin],
        _ => throw new ArgumentOutOfRangeException(nameof(politica), politica, "Política desconocida")
    };
}
