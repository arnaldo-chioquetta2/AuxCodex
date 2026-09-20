using System.Text;

namespace AuxCodex.Services;

public sealed class BatFileService
{
    private static readonly Encoding BatEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private readonly IFileSystemService _fileSystem;

    public BatFileService(IFileSystemService? fileSystem = null) => _fileSystem = fileSystem ?? new PhysicalFileSystemService();
    public string ReadContent(string path) => _fileSystem.ReadAllText(path, BatEncoding);
    public void WriteContent(string path, string content) => _fileSystem.WriteAllText(path, content, BatEncoding);
}