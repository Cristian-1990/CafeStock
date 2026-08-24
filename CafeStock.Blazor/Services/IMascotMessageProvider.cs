namespace CafeStock.Blazor.Services;

public interface IMascotMessageProvider
{
    /// <summary>
    /// Pose y mensaje de la mascota para una ruta relativa (sin "/" inicial, tal como la
    /// devuelve NavigationManager.ToBaseRelativePath). Si la ruta no tiene mensaje asignado
    /// devuelve null — el componente que lo consume simplemente no muestra nada, no rellena
    /// con un mensaje genérico de relleno.
    /// </summary>
    (MascotPose Pose, string Mensaje)? Get(string relativePath);
}
