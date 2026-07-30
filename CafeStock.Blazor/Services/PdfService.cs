using CafeStock.Back.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CafeStock.Blazor.Services;

public class PdfService
{
    public byte[] GenerarListaCompra(IEnumerable<Producto> productos)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var documento = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(14));

                page.Header()
                    .Text("Lista de la compra")
                    .FontSize(24)
                    .Bold()
                    .FontColor("#3E2723");

                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(1);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background("#6D4C41").Padding(8)
                                .Text("Producto").FontColor("#FFFFFF").Bold();
                            header.Cell().Background("#6D4C41").Padding(8)
                                .Text("Cantidad").FontColor("#FFFFFF").Bold();
                        });

                        foreach (var producto in productos)
                        {
                            table.Cell().BorderBottom(1).BorderColor("#D7CCC8").Padding(8)
                                .Text(producto.Nombre);
                            table.Cell().BorderBottom(1).BorderColor("#D7CCC8").Padding(8)
                                .Text(producto.CantidadAComprar.ToString());
                        }
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(x =>
                    {
                        x.Span("Generado el ");
                        x.Span(DateTime.Now.ToString("dd/MM/yyyy")).Bold();
                    });
            });
        });

        return documento.GeneratePdf();
    }
}