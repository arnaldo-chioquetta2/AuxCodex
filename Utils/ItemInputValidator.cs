namespace AuxCodex.Utils;

public static class ItemInputValidator
{
    public static bool TryValidateGptUrl(string? value, out string errorMessage)
    {
        errorMessage = string.Empty;
        var normalizedUrl = value?.Trim() ?? string.Empty;
        if (normalizedUrl.Length == 0)
        {
            return true;
        }

        if (!Uri.TryCreate(normalizedUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            errorMessage = "Informe uma URL válida para o GPT.";
            return false;
        }

        return true;
    }

    public static bool TryValidateBatPath(string? value, out string errorMessage)
    {
        errorMessage = string.Empty;
        var normalizedPath = value?.Trim() ?? string.Empty;
        if (normalizedPath.Length == 0)
        {
            errorMessage = "Selecione o arquivo BAT.";
            return false;
        }

        if (!string.Equals(Path.GetExtension(normalizedPath), ".bat", StringComparison.OrdinalIgnoreCase))
        {
            errorMessage = "O arquivo selecionado deve possuir extensão .bat.";
            return false;
        }

        var directory = Path.GetDirectoryName(normalizedPath);
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            errorMessage = "O diretório do arquivo BAT não existe.";
            return false;
        }

        return true;
    }
}
