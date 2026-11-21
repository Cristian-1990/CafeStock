using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CafeStock.Back.Migrations
{
    /// <summary>
    /// Colores de marca de cinco proveedores más, a petición del dueño: Atrian marrón oscuro (como su
    /// logo, más oscuro que Puchero), Délifrance azul oscuro, Mahou marrón claro (como sus botellines,
    /// más claro que Puchero), Pascual azul y Meritem naranja. Solo datos, sin cambios de esquema.
    ///
    /// Como en RecolorProveedores, los nombres se comparan AQUÍ, una sola vez, como corrección puntual
    /// de datos: la aplicación no decide nada por el nombre y el color se cambia a mano en el
    /// formulario del proveedor.
    /// </summary>
    public partial class ColoresDeMarcaProveedores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE Proveedores SET Color = '#5C3317' WHERE LOWER(TRIM(Nombre)) = 'atrian';");
            migrationBuilder.Sql("UPDATE Proveedores SET Color = '#1D3C87' WHERE LOWER(TRIM(Nombre)) IN ('delifrance', 'délifrance');");
            migrationBuilder.Sql("UPDATE Proveedores SET Color = '#B8732F' WHERE LOWER(TRIM(Nombre)) = 'mahou';");
            migrationBuilder.Sql("UPDATE Proveedores SET Color = '#1C7ED6' WHERE LOWER(TRIM(Nombre)) = 'pascual';");
            migrationBuilder.Sql("UPDATE Proveedores SET Color = '#F2711C' WHERE LOWER(TRIM(Nombre)) = 'meritem';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Sin vuelta atrás de datos: el color es solo presentación y se puede cambiar a mano.
        }
    }
}
