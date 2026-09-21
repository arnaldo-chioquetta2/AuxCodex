namespace AuxCodex.Models;

public sealed class SessionSecondaryProviderEditData
{
    public Guid ProviderDefinitionId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string ResumeKey { get; set; } = string.Empty;
    public ProviderLaunchConfiguration LaunchConfiguration { get; set; } = new();
    public string BatContent { get; set; } = string.Empty;
    public bool ManagedPath { get; set; }

    public SessionSecondaryProviderEditData Clone() => new()
    {
        ProviderDefinitionId = ProviderDefinitionId, ProviderName = ProviderName, ResumeKey = ResumeKey,
        LaunchConfiguration = LaunchConfiguration.Clone(), BatContent = BatContent, ManagedPath = ManagedPath
    };
}

public sealed class SessionEditData
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string OpenAiResumeKey { get; set; } = string.Empty;
    public ProviderLaunchConfiguration OpenAi { get; set; } = new();
    public string OpenAiBatContent { get; set; } = string.Empty;
    public bool OpenAiManagedPath { get; set; }
    public List<SessionSecondaryProviderEditData> SecondaryProviders { get; set; } = new();

    public SessionEditData Clone() => new()
    {
        Id = Id, Name = Name, OpenAiResumeKey = OpenAiResumeKey, OpenAi = OpenAi.Clone(),
        OpenAiBatContent = OpenAiBatContent, OpenAiManagedPath = OpenAiManagedPath, SecondaryProviders = (SecondaryProviders ?? new()).Select(value => value.Clone()).ToList()
    };
}
