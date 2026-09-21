using System.Text.RegularExpressions;

namespace AuxCodex.Services;

public sealed class BatWorkingDirectoryService
{
    private static readonly Regex CdLine = new(
        @"^(?<indent>[ \t]*)cd(?<switch>[ \t]+/d)?[ \t]+(?<path>""[^""\r\n]*""|[^\s\r\n]+)(?<tail>[^\r\n]*)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline);

    private static readonly Regex DriveLine = new(@"^[ \t]*(?<drive>[A-Za-z]):[ \t]*$", RegexOptions.Compiled);
    private static readonly Regex AbsolutePathLine = new(
        "^[ \\t]?[\\\"']?[A-Za-z]:\\\\[^\\r\\n]*[\\\"']?[ \\t]*$",
        RegexOptions.Compiled);

    public string UpdateProjectDirectory(string? content, string? projectDirectory)
    {
        if (string.IsNullOrEmpty(content) || string.IsNullOrWhiteSpace(projectDirectory)) return content ?? string.Empty;

        var fullPath = projectDirectory.Trim();
        try { fullPath = Path.GetFullPath(fullPath); }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return content;
        }

        var match = CdLine.Match(content);
        while (match.Success)
        {
            var line = content[match.Index..(match.Index + match.Length)];
            var trimmed = line.TrimStart();
            if (!trimmed.StartsWith("rem ", StringComparison.OrdinalIgnoreCase) &&
                !trimmed.Equals("rem", StringComparison.OrdinalIgnoreCase) &&
                !trimmed.StartsWith("::", StringComparison.Ordinal))
            {
                var oldPath = match.Groups["path"].Value;
                var quoted = oldPath.Length >= 2 && oldPath[0] == '"' && oldPath[^1] == '"';
                var hasSwitch = match.Groups["switch"].Success;
                var replacementPath = fullPath;

                if (!hasSwitch && oldPath.StartsWith("\\", StringComparison.Ordinal) && fullPath.Length >= 3 && fullPath[1] == ':')
                {
                    replacementPath = fullPath[2..];
                    var previousLineStart = content.LastIndexOf('\n', match.Index - 1);
                    previousLineStart = previousLineStart < 0 ? 0 : previousLineStart + 1;
                    var previousLineEnd = match.Index > 0 && content[match.Index - 1] == '\n' ? match.Index - 1 : match.Index;
                    var previousLine = content[previousLineStart..previousLineEnd].TrimEnd('\r');
                    var driveMatch = DriveLine.Match(previousLine);
                    if (driveMatch.Success && !driveMatch.Groups["drive"].Value.Equals(fullPath[0].ToString(), StringComparison.OrdinalIgnoreCase))
                    {
                        var newDriveLine = previousLine[..driveMatch.Groups["drive"].Index] + fullPath[0] + ":" + previousLine[(driveMatch.Groups["drive"].Index + 2)..];
                        content = content[..previousLineStart] + newDriveLine + content[previousLineEnd..];
                        match = CdLine.Match(content, match.Index + newDriveLine.Length - previousLine.Length);
                        continue;
                    }
                }

                if (quoted || replacementPath.Any(char.IsWhiteSpace)) replacementPath = $"\"{replacementPath.Trim('"')}\"";
                var replacement = match.Groups["indent"].Value + "cd" + match.Groups["switch"].Value + " " + replacementPath + match.Groups["tail"].Value;
                return content[..match.Index] + replacement + content[(match.Index + match.Length)..];
            }

            match = match.NextMatch();
        }

        return content;
    }

    public string ApplyGeneratedProjectDirectory(string? templateContent, string? projectDirectory)
    {
        var content = RemoveGeneratedDirectoryStructure(templateContent ?? string.Empty);
        if (string.IsNullOrWhiteSpace(projectDirectory)) return content;
        var normalized = projectDirectory.Trim();
        try { normalized = Path.GetFullPath(normalized); }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException) { return content; }
        if (!TryGetDriveWorkingDirectory(normalized, out var drive, out var directoryWithoutDrive)) return content;
        content = content.Replace("{{PROJECT_DIRECTORY}}", normalized, StringComparison.OrdinalIgnoreCase);
        var cdPath = directoryWithoutDrive.Any(char.IsWhiteSpace) ? $"\"{directoryWithoutDrive}\"" : directoryWithoutDrive;
        return $"{drive}\r\ncd {cdPath}\r\n{content}";
    }

    public bool SupportsGeneratedProjectDirectory(string? projectDirectory)
    {
        if (string.IsNullOrWhiteSpace(projectDirectory)) return true;
        try
        {
            return TryGetDriveWorkingDirectory(Path.GetFullPath(projectDirectory.Trim()), out _, out _);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }
    }

    private static bool TryGetDriveWorkingDirectory(string fullPath, out string drive, out string directoryWithoutDrive)
    {
        drive = string.Empty;
        directoryWithoutDrive = string.Empty;
        var root = Path.GetPathRoot(fullPath);
        if (string.IsNullOrWhiteSpace(root) || root.Length < 3 || root[1] != ':' || root[2] != '\\') return false;

        drive = root[..2];
        var remainder = fullPath[root.Length..];
        directoryWithoutDrive = remainder.Length == 0 ? "\\" : $"\\{remainder}";
        return true;
    }

    private static string RemoveGeneratedDirectoryStructure(string content)
    {
        if (content.Length == 0) return content;
        var lines = ReadLines(content);
        var start = 0;
        while (start < lines.Count && string.IsNullOrWhiteSpace(lines[start].Text)) start++;
        var commandIndex = start;
        if (commandIndex < lines.Count && IsEchoOff(lines[commandIndex].Text)) commandIndex++;
        if (commandIndex < lines.Count && IsDriveLine(lines[commandIndex].Text) && commandIndex + 1 < lines.Count && (IsAbsolutePathLine(lines[commandIndex + 1].Text) || IsCdLine(lines[commandIndex + 1].Text)))
        {
            lines.RemoveAt(commandIndex + 1);
            lines.RemoveAt(commandIndex);
            return JoinLines(lines);
        }
        if (commandIndex < lines.Count && IsCdLine(lines[commandIndex].Text))
        {
            lines.RemoveAt(commandIndex);
            return JoinLines(lines);
        }
        return content;
    }

    private static bool IsEchoOff(string line) => line.Trim().Equals("@echo off", StringComparison.OrdinalIgnoreCase);
    private static bool IsDriveLine(string line) => DriveLine.IsMatch(line);
    private static bool IsAbsolutePathLine(string line) => AbsolutePathLine.IsMatch(line);
    private static bool IsCdLine(string line)
    {
        var value = line.TrimStart();
        return value.Equals("cd", StringComparison.OrdinalIgnoreCase) || value.StartsWith("cd ", StringComparison.OrdinalIgnoreCase);
    }

    private static List<(string Text, string Ending)> ReadLines(string content)
    {
        var result = new List<(string, string)>();
        var start = 0;
        while (start < content.Length)
        {
            var index = content.IndexOfAny(new[] { '\r', '\n' }, start);
            if (index < 0) { result.Add((content[start..], string.Empty)); break; }
            var endingLength = content[index] == '\r' && index + 1 < content.Length && content[index + 1] == '\n' ? 2 : 1;
            result.Add((content[start..index], content.Substring(index, endingLength)));
            start = index + endingLength;
        }
        if (content.Length == 0) result.Add((string.Empty, string.Empty));
        return result;
    }

    private static string JoinLines(IEnumerable<(string Text, string Ending)> lines) => string.Concat(lines.Select(line => line.Text + line.Ending));
}
