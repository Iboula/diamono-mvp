using Diamono.Application.Payments;
using Diamono.Domain.Payments;
using Diamono.Infrastructure.Payments;
using Xunit;

namespace Diamono.Web.Tests;

public sealed class PaymentReceiptRendererTests
{
    [Fact]
    public void Questpdf_renderer_produit_un_pdf_non_vide()
    {
        var renderer = new QuestPdfPaymentReceiptRenderer();

        var content = renderer.Render(new PaymentReceiptData(
            "RCT-2026-000123",
            "PAY-2026-000123",
            "DIA-2026-000123",
            new DateTimeOffset(2026, 8, 15, 19, 0, 0, TimeSpan.Zero),
            "Awa Diop",
            "+221 77 000 00 00",
            new DateTimeOffset(2026, 8, 20, 18, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 8, 20, 20, 0, 0, TimeSpan.Zero),
            "Football",
            PaymentMethod.Cash,
            75_000m,
            Payment.MvpCurrency,
            "PAYE"));

        Assert.NotEmpty(content);
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(content, 0, 4));
    }
}
