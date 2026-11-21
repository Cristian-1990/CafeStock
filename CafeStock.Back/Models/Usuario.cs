namespace CafeStock.Back.Models;

/// <summary>Usuario de la aplicación tal y como lo ve la interfaz (sin hash de contraseña ni datos de Identity).</summary>
public record Usuario(string Id, string NombreUsuario, string Rol);
