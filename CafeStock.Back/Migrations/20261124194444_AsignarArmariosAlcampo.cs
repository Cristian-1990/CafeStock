using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CafeStock.Back.Migrations
{
    /// <summary>
    /// Reparte en armarios los productos de los supermercados genéricos (hoy Alcampo) que ya existen.
    /// Solo datos, sin cambios de esquema.
    ///
    /// Los nombres de producto se comparan AQUÍ, una sola vez, como corrección de datos puntual (mismo
    /// caso que RecolorProveedores): en ningún sitio de la aplicación se decide nada por el nombre, y el
    /// armario se puede cambiar a mano en el formulario de producto. El proveedor NO se identifica por
    /// nombre sino por su campo EsSupermercadoGenerico. Los productos que no existan con ese nombre
    /// exacto (salvo mayúsculas y espacios) simplemente no se tocan, y los que no aparecen aquí quedan
    /// sin armario.
    /// </summary>
    public partial class AsignarArmariosAlcampo : Migration
    {
        private static readonly (int Armario, string[] Productos)[] Reparto =
        [
            (1, ["Helado", "Mahou sin gluten"]),
            (2, ["Harina de trigo Gallo 2,5 Kg", "Harina de fuerza", "Azúcar blanco", "Azúcar moreno",
                 "Aceite de girasol", "Huevos", "Atún", "Sal", "Vainilla", "Crema de cacahuete",
                 "Chocolate con leche", "Miel", "Copos de avena", "Pasas", "Nueces", "Levadura en polvo",
                 "Azúcar glass", "Azúcar moscovado"]),
            (3, ["Manzanilla", "Frutas del bosque", "Limón y melisa", "Piña colada", "Poleo"]),
            (4, ["Pavo", "Jamón york", "Queso Gouda", "Salmón", "Aceitunas", "Mayonesa", "Balsámico",
                 "Roquefort", "Tomate Cherry"]),
            (5, ["Leche condensada", "Zumo Smoothie", "Yogur", "Mantequilla", "Margarina", "Rucula", "Lechuga"]),
            (6, ["Bitter", "Tonica", "Blue Moon", "Cibeles", "IPA"]),
            (7, ["Parmesano", "Gran Padano", "Hummus", "Levadura Fresca", "Gazpacho", "Zumo de naranja"]),
            (8, ["Chocolate valor 70%", "Nesquick", "Patatas fritas", "Arroz blanco", "Crema de verduras",
                 "Colacao", "Galleta"]),
            (9, ["Servilleta Grande", "Servilletas pequeñas", "Fairy", "Bolsas de basura grande",
                 "Bolsas de basura pequeñas", "Estropajo", "Cutty Shark", "Soberano", "Orégano",
                 "Mix de frutos secos", "Queso Crema"]),
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            foreach (var (armario, productos) in Reparto)
            {
                foreach (var nombre in productos)
                {
                    migrationBuilder.Sql(
                        $"UPDATE Productos SET Armario = {armario} " +
                        "WHERE Armario IS NULL " +
                        "AND ProveedorId IN (SELECT Id FROM Proveedores WHERE EsSupermercadoGenerico = 1) " +
                        $"AND LOWER(TRIM(Nombre)) = '{nombre.ToLowerInvariant().Replace("'", "''")}';");
                }
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Sin vuelta atrás de datos: el armario es solo organización y se puede cambiar a mano.
        }
    }
}
