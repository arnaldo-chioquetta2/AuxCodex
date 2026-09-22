using System.ComponentModel;

namespace AuxCodex.Forms;

public partial class FolderNameForm : Form
{
    private readonly bool _showProjectDirectory;
    private readonly string _initialProjectDirectory;

    public FolderNameForm(string title, string initialName = "", string initialProjectDirectory = "", bool showProjectDirectory = true)
    {
        InitializeComponent();
        Icon = Utils.ApplicationIconProvider.Icon;
        Text = title;
        _showProjectDirectory = showProjectDirectory;
        _initialProjectDirectory = initialProjectDirectory;
        nameTextBox.Text = initialName;
        projectDirectoryTextBox.Text = initialProjectDirectory;
        ConfigureProjectDirectoryVisibility();
        nameTextBox.SelectAll();
        Resize += OnFormResize;
    }

    public string FolderName { get; private set; } = string.Empty;
    public string ProjectDirectory { get; private set; } = string.Empty;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal Func<string, string?>? Validation { get; set; }

    private void OnSaveButtonClick(object? sender, EventArgs e)
    {
        var normalizedName = nameTextBox.Text.Trim();
        if (normalizedName.Length == 0)
        {
            ShowValidationError("Informe o nome da pasta.");
            return;
        }

        var validationError = Validation?.Invoke(normalizedName);
        if (!string.IsNullOrEmpty(validationError))
        {
            ShowValidationError(validationError);
            return;
        }

        var directoryText = projectDirectoryTextBox.Text.Trim();
        var normalizedDirectory = _showProjectDirectory ? string.Empty : _initialProjectDirectory;
        if (_showProjectDirectory && directoryText.Length > 0)
        {
            try
            {
                normalizedDirectory = NormalizeDirectory(directoryText);
                if (!Directory.Exists(normalizedDirectory))
                {
                    ShowValidationError("A Pasta do projeto deve apontar para um diretório existente.");
                    projectDirectoryTextBox.Focus();
                    projectDirectoryTextBox.SelectAll();
                    return;
                }
            }
            catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
            {
                ShowValidationError("A Pasta do projeto informada é inválida.");
                projectDirectoryTextBox.Focus();
                projectDirectoryTextBox.SelectAll();
                return;
            }
        }

        FolderName = normalizedName;
        ProjectDirectory = normalizedDirectory;
        DialogResult = DialogResult.OK;
        Close();
    }

    private void OnSelectProjectDirectoryClick(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Selecione a Pasta do projeto",
            ShowNewFolderButton = false
        };
        var current = projectDirectoryTextBox.Text.Trim();
        if (Directory.Exists(current)) dialog.SelectedPath = current;
        if (dialog.ShowDialog(this) == DialogResult.OK) projectDirectoryTextBox.Text = dialog.SelectedPath;
    }

    private void OnFormResize(object? sender, EventArgs e)
    {
        projectDirectoryTextBox.Width = Math.Max(80, selectProjectDirectoryButton.Left - projectDirectoryTextBox.Left - 6);
    }

    private void ConfigureProjectDirectoryVisibility()
    {
        if (_showProjectDirectory)
        {
            return;
        }

        projectDirectoryLabel.Visible = false;
        projectDirectoryTextBox.Visible = false;
        selectProjectDirectoryButton.Visible = false;
        validationErrorLabel.Location = new Point(18, 78);
        cancelButton.Location = new Point(191, 112);
        saveButton.Location = new Point(281, 112);
        ClientSize = new Size(384, 159);
    }

    private static string NormalizeDirectory(string value)
    {
        var fullPath = Path.GetFullPath(value.Trim());
        var root = Path.GetPathRoot(fullPath);
        return string.Equals(fullPath, root, StringComparison.OrdinalIgnoreCase)
            ? fullPath
            : fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    private void ShowValidationError(string message)
    {
        validationErrorLabel.Text = message;
        validationErrorLabel.Visible = true;
        nameTextBox.Focus();
        nameTextBox.SelectAll();
    }
}
