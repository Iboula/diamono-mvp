using Diamono.Application.Payments;
using Diamono.Domain.Payments;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Diamono.Infrastructure.Payments;

public sealed class QuestPdfPaymentReceiptRenderer : IPaymentReceiptRenderer
{
    static QuestPdfPaymentReceiptRenderer()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Render(PaymentReceiptData receipt)
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
                    column.Item().PaddingTop(4).Text("Reçu de paiement")
                        .FontSize(15)
                        .SemiBold();
                    column.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                });

                page.Content().PaddingVertical(22).Column(column =>
                {
                    column.Spacing(16);

                    column.Item().Border(1).BorderColor(Colors.Green.Lighten2).Padding(14).Column(section =>
                    {
                        section.Spacing(6);
                        Row(section, "Référence reçu", receipt.ReceiptReference);
                        Row(section, "Référence paiement", receipt.PaymentReference);
                        Row(section, "Référence réservation", receipt.BookingReference);
                        Row(section, "Date paiement", receipt.PaymentDate.ToString("dd/MM/yyyy HH:mm"));
                        Row(section, "Statut", receipt.Status);
                    });

                    column.Item().Element(e => Section(e, "Client", section =>
                    {
                        Row(section, "Client", receipt.CustomerName);
                        Row(section, "Téléphone", receipt.Phone);
                    }));

                    column.Item().Element(e => Section(e, "Réservation", section =>
                    {
                        Row(section, "Date réservation", receipt.BookingStartsAt.ToString("dd/MM/yyyy"));
                        Row(section, "Heure début", receipt.BookingStartsAt.ToString("HH:mm"));
                        Row(section, "Heure fin", receipt.BookingEndsAt.ToString("HH:mm"));
                        Row(section, "Activité", receipt.ActivityType);
                    }));

                    column.Item().Element(e => Section(e, "Paiement", section =>
                    {
                        Row(section, "Méthode paiement", MethodLabel(receipt.Method));
                        Row(section, "Montant payé", $"{receipt.Amount:N0}");
                        Row(section, "Devise", $"{receipt.Currency} / F CFA");
                    }));

                    column.Item().PaddingTop(8).Background(Colors.Grey.Lighten4).Padding(12).Text(
                        "Document de démonstration / format provisoire. Le modèle officiel mairie n'est pas encore validé.")
                        .FontSize(10)
                        .Italic();
                });

                page.Footer().Column(column =>
                {
                    column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                    column.Item().PaddingTop(8).Text("Reçu généré par la plateforme de gestion du Stade Diamono.")
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
            row.ConstantItem(150).Text(label).FontColor(Colors.Grey.Darken1);
            row.RelativeItem().Text(value).SemiBold();
        });

    private static string MethodLabel(PaymentMethod method)
        => method switch
        {
            PaymentMethod.Cash => "Espèces",
            PaymentMethod.MobileMoney => "Mobile Money",
            PaymentMethod.BankTransfer => "Virement",
            _ => method.ToString()
        };
}
