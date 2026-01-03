namespace Roster.Api.Models;

public class Store
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = String.Empty;
    public string Name { get; set; } = String.Empty;
}
