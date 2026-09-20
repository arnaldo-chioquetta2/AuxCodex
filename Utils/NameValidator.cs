namespace AuxCodex.Utils;

public static class NameValidator
{
    public static bool TryNormalize(string? value, out string normalizedName)
    {
        normalizedName = value?.Trim() ?? string.Empty;
        return normalizedName.Length > 0;
    }
}
