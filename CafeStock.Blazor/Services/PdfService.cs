using CafeStock.Back.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CafeStock.Blazor.Services;

public class PdfService
{
    private readonly IWebHostEnvironment _webHostEnvironment;

    public PdfService(IWebHostEnvironment webHostEnvironment)
    {
        _webHostEnvironment = webHostEnvironment;
    }

    public byte[] GenerarListaCompra(IEnumerable<Producto> productos)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var logoBytes = LeerLogo();

        var documento = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(12));
                page.PageColor("#FFFBF5");

                page.Header()
                    .Background("#E3D2AE")
                    .Padding(16)
                    .Row(row =>
                    {
                        if (logoBytes is not null)
                            row.ConstantItem(60).Height(60).Image(logoBytes).FitArea();

                        row.RelativeItem().PaddingLeft(logoBytes is not null ? 12 : 0).Column(column =>
                        {
                            column.Item().Text("Lista de la compra").FontSize(22).Bold().FontColor("#3E2723");
                            column.Item().AlignCenter().Text($"{DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(11).FontColor("#6D4C41");
                        });
                    });

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
                            header.Cell().Background("#E9DCC4").Padding(8)
                                .Text("Producto").FontColor("#3E2723").Bold();
                            header.Cell().Background("#E9DCC4").Padding(8)
                                .Text("Cantidad").FontColor("#3E2723").Bold();
                        });

                        foreach (var producto in productos)
                        {
                            table.Cell().BorderBottom(1).BorderColor("#E3D2AE").Padding(8)
                                .Text(producto.Nombre).FontColor("#2C2C2A");
                            table.Cell().BorderBottom(1).BorderColor("#E3D2AE").Padding(8)
                                .Text(producto.CantidadAComprar.ToString()).FontColor("#2C2C2A");
                        }
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(x =>
                    {
                        x.Span("Página ");
                        x.CurrentPageNumber();
                        x.Span(" de ");
                        x.TotalPages();
                    });
            });
        });

        return documento.GeneratePdf();
    }

    /// <summary>
    /// Genera el informe de pedido agrupado por proveedor: nombre del proveedor remarcado
    /// seguido de sus líneas "producto — cantidad"; los productos sin proveedor van al final
    /// bajo "Sin proveedor asignado".
    /// </summary>
    public byte[] GenerarInformePedido(IEnumerable<Producto> productos, IEnumerable<Proveedor> proveedores)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var proveedoresPorId = proveedores.ToDictionary(p => p.Id);
        var grupos = productos
            .GroupBy(p => p.ProveedorId.HasValue && proveedoresPorId.ContainsKey(p.ProveedorId.Value) ? p.ProveedorId : null)
            .OrderBy(g => g.Key is null)
            .ThenBy(g => g.Key.HasValue ? proveedoresPorId[g.Key.Value].Nombre : string.Empty);

        var logoBytes = LeerLogo();

        var documento = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(12));
                page.PageColor("#FFFBF5");

                page.Header()
                    .Background("#E3D2AE")
                    .Padding(16)
                    .Row(row =>
                    {
                        if (logoBytes is not null)
                            row.ConstantItem(60).Height(60).Image(logoBytes).FitArea();

                        row.RelativeItem().PaddingLeft(logoBytes is not null ? 12 : 0).Column(column =>
                        {
                            column.Item().Text("Pedido por proveedor").FontSize(22).Bold().FontColor("#3E2723");
                            column.Item().AlignCenter().Text($"{DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(11).FontColor("#6D4C41");
                        });
                    });

                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(column =>
                    {
                        column.Spacing(14);
                        foreach (var grupo in grupos)
                        {
                            var titulo = grupo.Key.HasValue ? proveedoresPorId[grupo.Key.Value].Nombre : "Sin proveedor asignado";
                            column.Item().Column(grupoColumn =>
                            {
                                grupoColumn.Item()
                                    .Background("#E9DCC4")
                                    .Padding(8)
                                    .Text(titulo).FontSize(16).Bold().FontColor("#3E2723");

                                foreach (var producto in grupo)
                                {
                                    grupoColumn.Item().PaddingLeft(12).PaddingTop(4)
                                        .Text($"{producto.Nombre} — {producto.CantidadAComprar}")
                                        .FontSize(12).FontColor("#2C2C2A");
                                }
                            });
                        }
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(x =>
                    {
                        x.Span("Página ");
                        x.CurrentPageNumber();
                        x.Span(" de ");
                        x.TotalPages();
                    });
            });
        });

        return documento.GeneratePdf();
    }

    private byte[]? LeerLogo()
    {
        var logoPath = Path.Combine(_webHostEnvironment.WebRootPath, "LogoCafeteria.png");
        return File.Exists(logoPath) ? File.ReadAllBytes(logoPath) : null;
    }
}