namespace AuxCodex.Models;

public sealed class TrayMenuEntryTag
{
    public TrayMenuEntryTag(Guid id, TrayMenuEntryType entryType) { Id = id; EntryType = entryType; }
    public TrayMenuEntryTag(Guid projectId, Guid sessionId) { Id = projectId; ProjectId = projectId; SessionId = sessionId; EntryType = TrayMenuEntryType.Session; }
    public TrayMenuEntryTag(Guid projectId, Guid sessionId, Guid? providerDefinitionId)
        : this(projectId, sessionId, providerDefinitionId, providerDefinitionId is null ? TrayMenuEntryType.OpenAiAction : TrayMenuEntryType.SecondaryProviderAction)
    {
    }

    public TrayMenuEntryTag(Guid projectId, Guid sessionId, Guid? providerDefinitionId, TrayMenuEntryType entryType)
    {
        Id = projectId; ProjectId = projectId; SessionId = sessionId; ProviderDefinitionId = providerDefinitionId;
        IsOpenAi = providerDefinitionId is null;
        HasProviderSelection = true;
        EntryType = entryType;
    }
    public Guid Id { get; }
    public Guid? ProjectId { get; }
    public Guid? SessionId { get; }
    public Guid? ProviderDefinitionId { get; }
    public bool IsOpenAi { get; }
    public TrayMenuEntryType EntryType { get; }
    private bool HasProviderSelection { get; }
    public bool IsExecutionEntry => HasProviderSelection && SessionId.HasValue &&
        (EntryType == TrayMenuEntryType.Item ||
         EntryType == TrayMenuEntryType.Session ||
         EntryType == TrayMenuEntryType.OpenAiAction ||
         EntryType == TrayMenuEntryType.SecondaryProviderAction);
}
