namespace AuxCodex.Models;

public sealed class SessionSecondaryProvider
{
    public Guid ProviderDefinitionId { get; set; }
    public string ResumeKey { get; set; } = string.Empty;
    public ProviderLaunchConfiguration LaunchConfiguration { get; set; } = new();
}