using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using Serilog;

namespace CafeStock.Blazor.Services;

public class EmailService
{
    private const string Placeholder = "PENDIENTE_CONFIGURAR";
    private readonly EmailSettings _settings;

    public EmailService(IOptions<EmailSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task EnviarListaCompraAsync(byte[] pdfAdjunto)
    {
        ValidarConfiguracion();

        var mensaje = new MimeMessage();
        mensaje.From.Add(MailboxAddress.Parse(_settings.Usuario));
        mensaje.To.Add(MailboxAddress.Parse(_settings.DestinatarioFijo));
        mensaje.Subject = $"Lista de la compra - {DateTime.Now:dd/MM/yyyy}";

        var cuerpo = new BodyBuilder { TextBody = "Adjunto la lista de la compra de CafeStock." };
        cuerpo.Attachments.Add("lista-compra.pdf", pdfAdjunto, ContentType.Parse("application/pdf"));
        mensaje.Body = cuerpo.ToMessageBody();

        await EnviarAsync(mensaje, "la lista de la compra");
    }

    /// <summary>
    /// Envía el informe de pedido agrupado por proveedor, generado al vuelo (sin adjunto,
    /// solo el texto del listado en el cuerpo del email).
    /// </summary>
    public async Task EnviarInformePedidoAsync(string cuerpoTexto)
    {
        ValidarConfiguracion();

        var mensaje = new MimeMessage();
        mensaje.From.Add(MailboxAddress.Parse(_settings.Usuario));
        mensaje.To.Add(MailboxAddress.Parse(_settings.DestinatarioFijo));
        mensaje.Subject = $"Pedido agrupado por proveedor - {DateTime.Now:dd/MM/yyyy}";
        mensaje.Body = new TextPart("plain") { Text = cuerpoTexto };

        await EnviarAsync(mensaje, "el informe de pedido por proveedor");
    }

    private async Task EnviarAsync(MimeMessage mensaje, string contextoError)
    {
        using var cliente = new SmtpClient();
        try
        {
            await cliente.ConnectAsync(_settings.SmtpHost, int.Parse(_settings.Puerto), SecureSocketOptions.StartTls);
            await cliente.AuthenticateAsync(_settings.Usuario, _settings.Contrasena);
            await cliente.SendAsync(mensaje);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error al enviar {ContextoError} por email", contextoError);
            throw new InvalidOperationException("No se pudo enviar el email. Revisa la configuración SMTP o la conexión a internet.", ex);
        }
        finally
        {
            if (cliente.IsConnected)
                await cliente.DisconnectAsync(true);
        }
    }

    /// <summary>
    /// Comprueba que la sección "Email" de appsettings.json está rellena antes de
    /// intentar conectar — así el usuario ve un mensaje claro en vez de un error
    /// de conexión/autenticación críptico cuando aún no ha puesto sus credenciales.
    /// </summary>
    private void ValidarConfiguracion()
    {
        var faltaAlgo =
            EstaSinConfigurar(_settings.SmtpHost) ||
            EstaSinConfigurar(_settings.Usuario) ||
            EstaSinConfigurar(_settings.Contrasena) ||
            EstaSinConfigurar(_settings.DestinatarioFijo) ||
            !int.TryParse(_settings.Puerto, out _);

        if (faltaAlgo)
            throw new InvalidOperationException(
                "El envío de email no está configurado todavía. Rellena la sección \"Email\" en appsettings.json.");
    }

    private static bool EstaSinConfigurar(string valor) =>
        string.IsNullOrWhiteSpace(valor) || valor == Placeholder;
}
