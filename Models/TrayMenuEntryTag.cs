namespace AuxCodex.Models;

public sealed class TrayMenuEntryTag
{
    public TrayMenuEntryTag(Guid id, TrayMenuEntryType entryType) { Id = id; EntryType = entryType; }
    public TrayMenuEntryTag(Guid projectId, Guid sessionId) { Id = projectId; ProjectId = projectId; SessionId = sessionId; EntryType = TrayMenuEntryType.Session; }
    public TrayMenuEntryTag(Guid projectId, Guid sessionId, Guid? providerDefinitionId)
    {
        Id = projectId; ProjectId = projectId; SessionId = sessionId; ProviderDefinitionId = providerDefinitionId;
        IsOpenAi = providerDefinitionId is null;
        EntryType = IsOpenAi ? TrayMenuEntryType.OpenAiAction : TrayMenuEntryType.SecondaryProviderAction;
    }
    public Guid Id { get; }
    public Guid? ProjectId { get; }
    public Guid? SessionId { get; }
    public Guid? ProviderDefinitionId { get; }
    public bool IsOpenAi { get; }
    public TrayMenuEntryType EntryType { get; }
}