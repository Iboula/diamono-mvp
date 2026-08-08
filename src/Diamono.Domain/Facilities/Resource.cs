namespace Diamono.Domain.Facilities;

public sealed class Resource
{
    private Resource() { }

    public Resource(Guid id, string name, TimeOnly opensAt, TimeOnly closesAt)
    {
        Id = id;
        Name = name;
        OpensAt = opensAt;
        ClosesAt = closesAt;
        IsBookable = true;
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public TimeOnly OpensAt { get; private set; }
    public TimeOnly ClosesAt { get; private set; }
    public bool IsBookable { get; private set; }
}
