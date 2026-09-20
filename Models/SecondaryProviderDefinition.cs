namespace AuxCodex.Models;

public sealed class SecondaryProviderDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string BatTemplate { get; set; } = string.Empty;
    public bool IsBuiltIn { get; set; }

    public SecondaryProviderDefinition Clone() => new() { Id = Id, Name = Name, BatTemplate = BatTemplate, IsBuiltIn = IsBuiltIn };
}