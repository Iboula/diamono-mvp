using System.Text;

namespace Diamono.Application.Notifications;

public sealed class PhoneNumberNormalizer : IPhoneNumberNormalizer
{
    public string NormalizeToE164(string phoneNumber, string defaultCountryCallingCode = "+221")
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new ArgumentException("Le numero de telephone est requis.", nameof(phoneNumber));

        if (string.IsNullOrWhiteSpace(defaultCountryCallingCode) || !defaultCountryCallingCode.StartsWith('+'))
            throw new ArgumentException("L'indicatif pays doit etre au format +XXX.", nameof(defaultCountryCallingCode));

        var trimmed = phoneNumber.Trim();
        var digits = new StringBuilder(trimmed.Length);
        var hasPlus = false;

        for (var i = 0; i < trimmed.Length; i++)
        {
            var c = trimmed[i];
            if (c == '+')
            {
                if (i != 0 || hasPlus)
                    throw new ArgumentException("Numero de telephone invalide.", nameof(phoneNumber));

                hasPlus = true;
                continue;
            }

            if (char.IsDigit(c))
            {
                digits.Append(c);
                continue;
            }

            if (char.IsWhiteSpace(c) || c is '-' or '.' or '(' or ')')
                continue;

            throw new ArgumentException("Numero de telephone invalide.", nameof(phoneNumber));
        }

        var normalized = hasPlus
            ? "+" + digits
            : defaultCountryCallingCode + digits;

        var digitCount = normalized.Count(char.IsDigit);
        if (digitCount is < 8 or > 15)
            throw new ArgumentException("Numero de telephone invalide.", nameof(phoneNumber));

        return normalized;
    }
}
