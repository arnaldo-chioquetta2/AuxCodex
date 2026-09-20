namespace AuxCodex.Services;

/// <summary>
/// Sincroniza a chave de resume entre o campo dedicado e o conteudo textual do BAT.
/// A deteccao depende apenas do token <c>resume</c> seguido de seu argumento,
/// ignorando caminhos, unidades, modelos e comandos comentados.
/// </summary>
public sealed class BatResumeKeyService
{
    private const string ResumeToken = "resume";

    private static readonly char[] QuoteCharacters = { '"', '\'' };

    /// <summary>
    /// Extrai a chave de resume do conteudo. Retorna <c>true</c> quando existe uma
    /// ocorrencia valida de <c>resume</c>; <paramref name="resumeKey"/> fica vazio
    /// quando o token existe sem argumento.
    /// </summary>
    public bool TryExtractResumeKey(string? content, out string resumeKey)
    {
        resumeKey = string.Empty;
        if (content is null)
        {
            return false;
        }

        var position = FindResumeToken(content);
        if (position < 0)
        {
            return false;
        }

        var argument = ReadArgument(content, position + ResumeToken.Length);
        resumeKey = argument?.Value ?? string.Empty;
        return true;
    }

    /// <summary>
    /// Substitui somente o valor associado ao token <c>resume</c>.
    /// Quando <paramref name="newResumeKey"/> e vazio, o argumento e removido mas o
    /// token e todo o restante da linha sao preservados.
    /// </summary>
    public string ReplaceResumeKey(string? content, string? newResumeKey)
    {
        if (content is null)
        {
            return string.Empty;
        }

        var position = FindResumeToken(content);
        if (position < 0)
        {
            return content;
        }

        var normalizedKey = NormalizeKey(newResumeKey);
        var argument = ReadArgument(content, position + ResumeToken.Length);
        var tokenEnd = position + ResumeToken.Length;

        // Fim do argumento; sem argumento, o token e seguido apenas por espacos.
        var argumentEnd = argument?.End ?? tokenEnd;
        var removeEnd = argumentEnd;
        while (removeEnd < content.Length && (content[removeEnd] == ' ' || content[removeEnd] == '\t'))
        {
            removeEnd++;
        }

        var deleted = content[tokenEnd..removeEnd];
        var leadingSpaces = deleted[..LeadingSpaceCount(deleted)];
        if (leadingSpaces.Length == 0)
        {
            // Sem espaco existente (token sem argumento): garante a separacao.
            leadingSpaces = " ";
        }

        string replacement;
        if (normalizedKey.Length == 0)
        {
            // Nenhum argumento deve permanecer: remove o valor, mas mantem o token
            // e a quebra de linha intactos.
            replacement = string.Empty;
        }
        else if (argument?.Quote is char quote)
        {
            var escaped = quote == '"'
                ? normalizedKey.Replace("\"", string.Empty)
                : normalizedKey.Replace("'", string.Empty);
            replacement = $"{leadingSpaces}{quote}{escaped}{quote}";
        }
        else
        {
            replacement = $"{leadingSpaces}{normalizedKey}";
        }

        return content[..tokenEnd] + replacement + content[removeEnd..];
    }

    /// <summary>
    /// Remove o valor de resume mantendo o token e o restante da linha intactos.
    /// Usada para transformar um BAT salvo em template reaproveitavel.
    /// </summary>
    public string RemoveResumeKey(string? content) => ReplaceResumeKey(content, string.Empty);

    private static int LeadingSpaceCount(string value)
    {
        var index = 0;
        while (index < value.Length && (value[index] == ' ' || value[index] == '\t'))
        {
            index++;
        }

        return index;
    }

    private static string NormalizeKey(string? value)
    {
        var normalized = value?.Trim() ?? string.Empty;
        return normalized.Replace("\r", string.Empty).Replace("\n", string.Empty);
    }


    /// <summary>
    /// Retorna a posicao inicial do primeiro token <c>resume</c> fora de comentarios,
    /// ou -1 quando nao existir.
    /// </summary>
    private static int FindResumeToken(string content)
    {
        var lineStart = 0;
        while (lineStart <= content.Length)
        {
            var lineEnd = content.IndexOf('\n', lineStart);
            if (lineEnd < 0)
            {
                lineEnd = content.Length;
            }

            var position = FindResumeTokenInLine(content, lineStart, lineEnd);
            if (position >= 0)
            {
                return position;
            }

            if (lineEnd >= content.Length)
            {
                break;
            }

            lineStart = lineEnd + 1;
        }

        return -1;
    }

    private static int FindResumeTokenInLine(string content, int lineStart, int lineEnd)
    {
        var cursor = lineStart;
        while (cursor < lineEnd && char.IsWhiteSpace(content[cursor]))
        {
            cursor++;
        }

        if (cursor >= lineEnd || IsCommentStart(content, cursor, lineEnd))
        {
            return -1;
        }

        for (var index = lineStart; index + ResumeToken.Length <= lineEnd; index++)
        {
            if (!IsTokenStart(content, index))
            {
                continue;
            }

            if (string.Compare(content, index, ResumeToken, 0, ResumeToken.Length, StringComparison.OrdinalIgnoreCase) != 0)
            {
                continue;
            }

            if (!IsTokenEnd(content, index + ResumeToken.Length))
            {
                continue;
            }

            return index;
        }

        return -1;
    }

    private static bool IsCommentStart(string content, int index, int lineEnd)
    {
        if (content[index] == ':')
        {
            return index + 1 < lineEnd && content[index + 1] == ':';
        }

        var remaining = lineEnd - index;
        if (remaining < 3)
        {
            return false;
        }

        if (string.Compare(content, index, "REM", 0, 3, StringComparison.OrdinalIgnoreCase) != 0)
        {
            return false;
        }

        var afterRem = index + 3;
        if (afterRem >= lineEnd)
        {
            return true;
        }

        var next = content[afterRem];
        return char.IsWhiteSpace(next) || next == ':';
    }

    private static bool IsTokenStart(string content, int index) =>
        index == 0 || !IsWordCharacter(content[index - 1]);

    private static bool IsTokenEnd(string content, int index) =>
        index >= content.Length || !IsWordCharacter(content[index]);

    private static bool IsWordCharacter(char character) =>
        char.IsLetterOrDigit(character) || character == '_' || character == '-';

    private static Argument? ReadArgument(string content, int afterToken)
    {
        var position = afterToken;
        while (position < content.Length && (content[position] == ' ' || content[position] == '\t'))
        {
            position++;
        }

        if (position >= content.Length || content[position] == '\r' || content[position] == '\n')
        {
            return null;
        }

        if (IsQuote(content[position]))
        {
            return ReadQuotedArgument(content, position);
        }

        return ReadPlainArgument(content, position);
    }

    private static Argument ReadQuotedArgument(string content, int start)
    {
        var quote = content[start];
        var valueStart = start + 1;
        var position = valueStart;

        while (position < content.Length)
        {
            var current = content[position];
            if (current == '\r' || current == '\n')
            {
                break;
            }

            if (current == quote)
            {
                var end = position + 1;
                return new Argument(start, end, content[valueStart..position], quote);
            }

            position++;
        }

        return new Argument(start, position, content[valueStart..position], null);
    }

    private static Argument? ReadPlainArgument(string content, int start)
    {
        var position = start;
        while (position < content.Length)
        {
            var current = content[position];
            if (current == ' ' || current == '\t' || current == '\r' || current == '\n')
            {
                break;
            }

            position++;
        }

        return position > start
            ? new Argument(start, position, content[start..position], null)
            : null;
    }

    private static bool IsQuote(char character) => Array.IndexOf(QuoteCharacters, character) >= 0;

    private sealed record Argument(int Start, int End, string Value, char? Quote);
}