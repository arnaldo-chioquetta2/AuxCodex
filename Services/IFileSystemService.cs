namespace AuxCodex.Services;

public interface IFileSystemService
{
    void CreateDirectory(string path);
    bool FileExists(string path);
    string ReadAllText(string path, System.Text.Encoding encoding);
    byte[] ReadAllBytes(string path);
    void WriteAllText(string path, string content, System.Text.Encoding encoding);
    void WriteAllBytes(string path, byte[] bytes);
    void Replace(string sourceFileName, string destinationFileName);
    void Move(string sourceFileName, string destinationFileName);
    void Copy(string sourceFileName, string destinationFileName);
    void Delete(string path);
}

public sealed class PhysicalFileSystemService : IFileSystemService
{
    public void CreateDirectory(string path) => Directory.CreateDirectory(path);
    public bool FileExists(string path) => File.Exists(path);
    public string ReadAllText(string path, System.Text.Encoding encoding) => File.ReadAllText(path, encoding);
    public byte[] ReadAllBytes(string path) => File.ReadAllBytes(path);
    public void WriteAllText(string path, string content, System.Text.Encoding encoding) => File.WriteAllText(path, content, encoding);
    public void WriteAllBytes(string path, byte[] bytes) => File.WriteAllBytes(path, bytes);
    public void Replace(string sourceFileName, string destinationFileName) => File.Replace(sourceFileName, destinationFileName, null);
    public void Move(string sourceFileName, string destinationFileName) => File.Move(sourceFileName, destinationFileName);
    public void Copy(string sourceFileName, string destinationFileName) => File.Copy(sourceFileName, destinationFileName);
    public void Delete(string path) => File.Delete(path);
}