using System.ComponentModel;

namespace AuxCodex.Forms;

public partial class SessionNameForm : Form
{
    public SessionNameForm(string title, string initialName = "")
    {
        InitializeComponent();
        Icon = Utils.ApplicationIconProvider.Icon;
        Text = title;
        nameTextBox.Text = initialName;
        nameTextBox.SelectAll();
    }

    public string SessionName { get; private set; } = string.Empty;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    internal Func<string, string?>? Validation { get; set; }

    private void OnSaveButtonClick(object? sender, EventArgs e)
    {
        var normalizedName = nameTextBox.Text.Trim();
        if (normalizedName.Length == 0)
        {
            ShowValidationError("Informe o nome da sessão.");
            return;
        }

        var validationError = Validation?.Invoke(normalizedName);
        if (!string.IsNullOrEmpty(validationError))
        {
            ShowValidationError(validationError);
            return;
        }

        SessionName = normalizedName;
        DialogResult = DialogResult.OK;
        Close();
    }

    private void ShowValidationError(string message)
    {
        validationErrorLabel.Text = message;
        validationErrorLabel.Visible = true;
        nameTextBox.Focus();
        nameTextBox.SelectAll();
    }
}
