namespace AuxCodex.Services;

public static class BatTemplateDefaults
{
    // Template mínimo, sem modelo, provedor específico ou chave de sessão.
    public const string OpenAi = "@echo off\r\ncd /d \"{{PROJECT_DIRECTORY}}\"\r\ncodex resume";
}
