using AuxCodex.Models;

namespace AuxCodex.Services;

public static class SecondaryProviderCatalogDefaults
{
    public static readonly Guid DeepSeekId = Guid.Parse("7c5af165-544c-4315-9a9f-5f3e8c211201");
    public static readonly Guid GlmId = Guid.Parse("7c5af165-544c-4315-9a9f-5f3e8c211202");
    public const string DeepSeekName = "DeepSeek";
    public const string GlmName = "GLM";
    public const string GlmTemplate = "@echo off\r\ncodex -m z-ai/glm-5.3-flash -c model_provider=\"openrouter\" resume\r\n";
    public const string DeepSeekTemplate = "@echo off\r\ncodex --model deepseek-chat resume\r\n";

    public static List<SecondaryProviderDefinition> CreateDefaults(string? legacyDeepSeekTemplate = null)
    {
        var resume = new BatResumeKeyService();
        return new List<SecondaryProviderDefinition>
        {
            new() { Id = DeepSeekId, Name = DeepSeekName, BatTemplate = resume.RemoveResumeKey(legacyDeepSeekTemplate ?? DeepSeekTemplate), IsBuiltIn = true },
            new() { Id = GlmId, Name = GlmName, BatTemplate = resume.RemoveResumeKey(GlmTemplate), IsBuiltIn = true }
        };
    }
}