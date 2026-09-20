#nullable enable

namespace AuxCodex.Forms;

partial class FolderNameForm
{
    private Label nameLabel = null!;
    private TextBox nameTextBox = null!;
    private Label validationErrorLabel = null!;
    private Button cancelButton = null!;
    private Button saveButton = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
        }

        base.Dispose(disposing);
    }

    private System.ComponentModel.IContainer? components;

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        nameLabel = new Label();
        nameTextBox = new TextBox();
        validationErrorLabel = new Label();
        cancelButton = new Button();
        saveButton = new Button();
        SuspendLayout();
        //
        // nameLabel
        //
        nameLabel.AutoSize = true;
        nameLabel.Location = new Point(18, 20);
        nameLabel.Name = "nameLabel";
        nameLabel.Size = new Size(43, 15);
        nameLabel.TabIndex = 0;
        nameLabel.Text = "Nome:";
        //
        // nameTextBox
        //
        nameTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        nameTextBox.Location = new Point(18, 42);
        nameTextBox.Name = "nameTextBox";
        nameTextBox.Size = new Size(348, 23);
        nameTextBox.TabIndex = 1;
        //
        // validationErrorLabel
        //
        validationErrorLabel.AutoSize = true;
        validationErrorLabel.ForeColor = Color.Firebrick;
        validationErrorLabel.Location = new Point(18, 72);
        validationErrorLabel.MaximumSize = new Size(348, 0);
        validationErrorLabel.Name = "validationErrorLabel";
        validationErrorLabel.Size = new Size(0, 15);
        validationErrorLabel.TabIndex = 2;
        validationErrorLabel.Visible = false;
        //
        // cancelButton
        //
        cancelButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        cancelButton.DialogResult = DialogResult.Cancel;
        cancelButton.Location = new Point(191, 104);
        cancelButton.Name = "cancelButton";
        cancelButton.Size = new Size(85, 28);
        cancelButton.TabIndex = 3;
        cancelButton.Text = "Cancelar";
        cancelButton.UseVisualStyleBackColor = true;
        //
        // saveButton
        //
        saveButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        saveButton.Location = new Point(281, 104);
        saveButton.Name = "saveButton";
        saveButton.Size = new Size(85, 28);
        saveButton.TabIndex = 4;
        saveButton.Text = "Salvar";
        saveButton.UseVisualStyleBackColor = true;
        saveButton.Click += OnSaveButtonClick;
        //
        // FolderNameForm
        //
        AcceptButton = saveButton;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = cancelButton;
        ClientSize = new Size(384, 151);
        Controls.Add(saveButton);
        Controls.Add(cancelButton);
        Controls.Add(validationErrorLabel);
        Controls.Add(nameTextBox);
        Controls.Add(nameLabel);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "FolderNameForm";
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Pasta";
        ResumeLayout(false);
        PerformLayout();
    }
}
