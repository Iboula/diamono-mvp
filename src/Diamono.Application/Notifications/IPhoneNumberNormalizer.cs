namespace Diamono.Application.Notifications;

public interface IPhoneNumberNormalizer
{
    string NormalizeToE164(string phoneNumber, string defaultCountryCallingCode = "+221");
}
