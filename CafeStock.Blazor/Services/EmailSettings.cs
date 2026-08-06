namespace CafeStock.Blazor.Services;

/// <summary>
/// Configuración SMTP para el envío de la lista de la compra por email.
/// Se rellena en appsettings.json (sección "Email").
/// </summary>
public class EmailSettings
{
    public string SmtpHost { get; set; } = string.Empty;
    public string Puerto { get; set; } = string.Empty;
    public string Usuario { get; set; } = string.Empty;
    public string Contrasena { get; set; } = string.Empty;
    public string DestinatarioFijo { get; set; } = string.Empty;
}
