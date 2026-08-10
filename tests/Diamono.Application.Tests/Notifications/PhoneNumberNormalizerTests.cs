using Diamono.Application.Notifications;
using Xunit;

namespace Diamono.Application.Tests;

public sealed class PhoneNumberNormalizerTests
{
    private readonly PhoneNumberNormalizer normalizer = new();

    [Fact]
    public void Numero_senegalais_est_normalise_en_e164()
    {
        var result = normalizer.NormalizeToE164("77 123 45 67");

        Assert.Equal("+221771234567", result);
    }

    [Fact]
    public void Numero_e164_reste_inchange()
    {
        var result = normalizer.NormalizeToE164("+14155550100");

        Assert.Equal("+14155550100", result);
    }

    [Fact]
    public void Numero_invalide_est_rejete()
    {
        Assert.Throws<ArgumentException>(() => normalizer.NormalizeToE164("abc-123"));
    }
}
