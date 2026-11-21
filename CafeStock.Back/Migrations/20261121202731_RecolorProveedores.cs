using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CafeStock.Back.Migrations
{
    /// <summary>
    /// Pasa los colores internos de los proveedores a una paleta más viva y fija los de marca de tres
    /// proveedores concretos. Solo datos, sin cambios de esquema.
    ///
    /// Los nombres Puchero, Coca-Cola y Alcampo se comparan AQUÍ, una sola vez, como corrección de
    /// datos puntual (mismo caso que las migraciones de datos de EsquemaHeredado): en ningún sitio de
    /// la aplicación se decide nada por el nombre, y el color se puede cambiar a mano en el formulario.
    /// </summary>
    public partial class RecolorProveedores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) Paleta antigua (AddColorProveedorYFichaCafe) -> paleta nueva, tono a tono.
            migrationBuilder.Sql(@"
                UPDATE Proveedores
                SET Color = CASE UPPER(Color)
                    WHEN '#A8481F' THEN '#E8590C'
                    WHEN '#3F6B2E' THEN '#2F9E44'
                    WHEN '#1F5F8B' THEN '#1C7ED6'
                    WHEN '#8E4585' THEN '#AE3EC9'
                    WHEN '#C17817' THEN '#F59F00'
                    WHEN '#2E7D7A' THEN '#0CA678'
                    WHEN '#B23A48' THEN '#E64980'
                    WHEN '#5D6D2B' THEN '#74B816'
                    WHEN '#6D4C41' THEN '#7048E8'
                    WHEN '#4A5FA5' THEN '#15AABF'
                    ELSE Color
                END
                WHERE UPPER(Color) IN ('#A8481F','#3F6B2E','#1F5F8B','#8E4585','#C17817','#2E7D7A','#B23A48','#5D6D2B','#6D4C41','#4A5FA5');");

            // 2) Colores de marca: Puchero marrón, Coca-Cola rojo intenso y Alcampo un rojo más suave.
            migrationBuilder.Sql("UPDATE Proveedores SET Color = '#8B4513' WHERE LOWER(TRIM(Nombre)) = 'puchero';");
            migrationBuilder.Sql("UPDATE Proveedores SET Color = '#F40009' WHERE LOWER(TRIM(Nombre)) = 'coca-cola';");
            migrationBuilder.Sql("UPDATE Proveedores SET Color = '#D9534F' WHERE LOWER(TRIM(Nombre)) = 'alcampo';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Sin vuelta atrás de datos: el color es solo presentación y se puede cambiar a mano.
        }
    }
}
