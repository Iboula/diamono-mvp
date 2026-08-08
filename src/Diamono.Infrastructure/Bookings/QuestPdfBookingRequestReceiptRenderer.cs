using Diamono.Application.Bookings;
using Diamono.Domain.Bookings;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Diamono.Infrastructure.Bookings;

public sealed class QuestPdfBookingRequestReceiptRenderer : IBookingRequestReceiptRenderer
{
    static QuestPdfBookingRequestReceiptRenderer()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Render(BookingRequestReceiptData receipt)
        => Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(42);
                page.DefaultTextStyle(x => x.FontSize(11).FontColor(Colors.Grey.Darken4));

                page.Header().Column(column =>
                {
                    column.Item().Text("STADE DIAMONO DE CAMBÉRÈNE")
                        .FontSize(20)
                        .Bold()
                        .FontColor(Colors.Green.Darken3);
                    column.Item().PaddingTop(4).Text("RECU PROVISOIRE")
                        .FontSize(16)
                        .SemiBold();
                    column.Item().Text("DE DEMANDE DE RÉSERVATION")
                        .FontSize(16)
                        .SemiBold();
                    column.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingVertical(22).Column(column =>
                {
                    column.Spacing(16);

                    column.Item().Background(Colors.Orange.Lighten4).Border(1).BorderColor(Colors.Orange.Lighten2).Padding(14).Column(section =>
                    {
                        section.Spacing(7);
                        Row(section, "Référence document", receipt.ReceiptReference);
                        Row(section, "Référence réservation", receipt.BookingReference);
                        Row(section, "Date de création", receipt.CreatedAt.ToString("dd/MM/yyyy HH:mm"));
                        Row(section, "Statut", receipt.StatusLabel);
                    });

                    column.Item().Element(e => Section(e, "Demandeur", section =>
                    {
                        Row(section, "Client", receipt.CustomerName);
                        Row(section, "Téléphone", receipt.Phone);
                        Row(section, "Catégorie client", CategoryLabel(receipt.CustomerCategory));
                    }));

                    column.Item().Element(e => Section(e, "Créneau demandé", section =>
                    {
                        Row(section, "Terrain / ressource", receipt.ResourceName);
                        Row(section, "Date demandée", receipt.StartsAt.ToString("dd/MM/yyyy"));
                        Row(section, "Heure début", receipt.StartsAt.ToString("HH:mm"));
                        Row(section, "Heure fin", receipt.EndsAt.ToString("HH:mm"));
                        Row(section, "Durée", $"{(receipt.EndsAt - receipt.StartsAt).TotalHours:0.#} h");
                        Row(section, "Activité", receipt.ActivityType);
                    }));

                    column.Item().Element(e => Section(e, "Montants estimatifs historiques", section =>
                    {
                        Row(section, "Location terrain", Money(receipt.RentalAmount));
                        Row(section, "Éclairage", Money(receipt.LightingAmount));
                        Row(section, "Caution estimative", Money(receipt.DepositAmount));
                        Row(section, "Total estimatif", Money(receipt.TotalAmount));
                    }));

                    column.Item().Background(Colors.Grey.Lighten4).Padding(14).Text(
                        "Ce document confirme uniquement la réception de votre demande. " +
                        "Il ne constitue pas une réservation confirmée. " +
                        "La réservation sera confirmée après validation par l'administration du stade et paiement.")
                        .FontSize(10)
                        .SemiBold();
                });

                page.Footer().Column(column =>
                {
                    column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                    column.Item().PaddingTop(8).Text("Document généré automatiquement par Diamono Platform.")
                        .FontSize(10)
                        .FontColor(Colors.Grey.Darken1);
                });
            });
        }).GeneratePdf();

    private static void Section(IContainer container, string title, Action<ColumnDescriptor> content)
        => container.Column(column =>
        {
            column.Spacing(6);
            column.Item().Text(title).FontSize(13).SemiBold().FontColor(Colors.Green.Darken2);
            column.Item().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingBottom(8).Column(content);
        });

    private static void Row(ColumnDescriptor column, string label, string value)
        => column.Item().Row(row =>
        {
            row.ConstantItem(170).Text(label).FontColor(Colors.Grey.Darken1);
            row.RelativeItem().Text(value).SemiBold();
        });

    private static string Money(decimal amount) => $"{amount:N0} F CFA";

    private static string CategoryLabel(CustomerCategory category)
        => category switch
        {
            CustomerCategory.Individual => "Particulier",
            CustomerCategory.LocalAsc => "ASC de Cambérène",
            CustomerCategory.Club => "Club",
            CustomerCategory.Association => "Association",
            CustomerCategory.School => "Ecole",
            CustomerCategory.Company => "Entreprise",
            _ => category.ToString()
        };
}
