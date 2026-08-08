namespace Diamono.Application.Reporting;

public sealed record ReportingPeriod(DateOnly StartsOn, DateOnly EndsOn)
{
    public void Deconstruct(out DateOnly startsOn, out DateOnly endsOn)
    {
        startsOn = StartsOn;
        endsOn = EndsOn;
    }
}
