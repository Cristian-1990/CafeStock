using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CafeStock.Back.Entity;

/// <summary>
/// Solo la usan las herramientas de EF Core en tiempo de diseño (<c>dotnet ef migrations add</c>,
/// <c>dotnet ef migrations script</c>…). <see cref="AppDbContext"/> no tiene constructor sin
/// parámetros (recibe la cadena de conexión), así que sin esta fábrica "dotnet ef" no sabe
/// cómo crearlo.
///
/// La base de datos apuntada es un fichero desechable en la carpeta temporal: generar una
/// migración solo necesita el modelo, nunca toca datos, y así es imposible que una herramienta
/// de diseño abra por error la base de datos real (cafestock.db).
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var ruta = Path.Combine(Path.GetTempPath(), "cafestock-design-time.db");
        return new AppDbContext($"Data Source={ruta}");
    }
}
