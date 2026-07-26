namespace FirearmStudio.Application.Model.Options;

public sealed class ForwardedHeadersSettings
{
    public const string SectionName = "ForwardedHeaders";

    public List<string> KnownNetworks { get; init; } = [];

    public List<string> KnownProxies { get; init; } = [];

    public int ForwardLimit { get; init; } = 1;
}
