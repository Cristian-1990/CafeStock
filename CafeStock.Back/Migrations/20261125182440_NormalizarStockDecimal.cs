using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CafeStock.Back.Migrations
{
    /// <summary>
    /// Deja los valores de stock antiguos en el formato en que EF escribe los decimales. Solo datos.
    ///
    /// SQLite guarda los decimales como texto y Microsoft.Data.Sqlite los escribe siempre con al menos un
    /// decimal ("1.0", nunca "1"). Al convertir las columnas de entero a decimal (migración StockDecimal)
    /// los valores antiguos quedaron como "1", y la comprobación de concurrencia del stock
    /// (WHERE StockActual = &lt;valor leído&gt;) compara texto con texto: "1" no coincide con "1.0" y cualquier
    /// actualización de un producto antiguo fallaría con un error de concurrencia. Va en una migración aparte
    /// porque EF reconstruye la tabla al final de StockDecimal: un UPDATE dentro de ella correría antes, con
    /// la columna todavía entera, y no tendría efecto. Idempotente: solo toca los valores sin punto decimal.
    /// </summary>
    public partial class NormalizarStockDecimal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE Productos SET StockActual = StockActual || '.0' WHERE INSTR(StockActual, '.') = 0;");
            migrationBuilder.Sql("UPDATE Productos SET StockMaximo = StockMaximo || '.0' WHERE INSTR(StockMaximo, '.') = 0;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Sin vuelta atrás de datos: "1.0" y "1" son el mismo valor.
        }
    }
}
