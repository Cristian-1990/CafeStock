namespace CafeStock.Back.Entity;

public class ProveedorEntity
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string ImagenUrl { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string PersonaContacto { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string DiaReparto { get; set; } = string.Empty;
    public string Notas { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public string NifCif { get; set; } = string.Empty;
    public bool EsSupermercadoGenerico { get; set; } = false;
}
