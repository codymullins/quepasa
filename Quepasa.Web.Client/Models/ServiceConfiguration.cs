namespace Quepasa.Web.Client.Models;

public record ServiceConfiguration
{
    public string? Name { get; set; }
    public required string Url { get; set; }
    public CheckType CheckType { get; set; }
}

public enum CheckType
{
    Default,
    StatusCode,
}