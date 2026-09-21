#nullable enable

namespace AuxCodex.Forms;

partial class FolderNameForm
{
    private Label nameLabel = null!;
    private TextBox nameTextBox = null!;
    private Label projectDirectoryLabel = null!;
    private TextBox projectDirectoryTextBox = null!;
    private Button selectProjectDirectoryButton = null!;
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
        projectDirectoryLabel = new Label();
        projectDirectoryTextBox = new TextBox();
        selectProjectDirectoryButton = new Button();
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
        // projectDirectoryLabel
        //
        projectDirectoryLabel.AutoSize = true;
        projectDirectoryLabel.Location = new Point(18, 75);
        projectDirectoryLabel.Name = "projectDirectoryLabel";
        projectDirectoryLabel.Size = new Size(101, 15);
        projectDirectoryLabel.TabIndex = 2;
        projectDirectoryLabel.Text = "Pasta do projeto:";
        //
        // projectDirectoryTextBox
        //
        projectDirectoryTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        projectDirectoryTextBox.Location = new Point(18, 97);
        projectDirectoryTextBox.Name = "projectDirectoryTextBox";
        projectDirectoryTextBox.Size = new Size(255, 23);
        projectDirectoryTextBox.TabIndex = 3;
        //
        // selectProjectDirectoryButton
        //
        selectProjectDirectoryButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        selectProjectDirectoryButton.Location = new Point(279, 96);
        selectProjectDirectoryButton.Name = "selectProjectDirectoryButton";
        selectProjectDirectoryButton.Size = new Size(87, 25);
        selectProjectDirectoryButton.TabIndex = 4;
        selectProjectDirectoryButton.Text = "Selecionar...";
        selectProjectDirectoryButton.UseVisualStyleBackColor = true;
        selectProjectDirectoryButton.Click += OnSelectProjectDirectoryClick;
        //
        // validationErrorLabel
        //
        validationErrorLabel.AutoSize = true;
        validationErrorLabel.ForeColor = Color.Firebrick;
        validationErrorLabel.Location = new Point(18, 133);
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
        cancelButton.Location = new Point(191, 190);
        cancelButton.Name = "cancelButton";
        cancelButton.Size = new Size(85, 28);
        cancelButton.TabIndex = 3;
        cancelButton.Text = "Cancelar";
        cancelButton.UseVisualStyleBackColor = true;
        //
        // saveButton
        //
        saveButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        saveButton.Location = new Point(281, 190);
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
        ClientSize = new Size(384, 237);
        Controls.Add(saveButton);
        Controls.Add(cancelButton);
        Controls.Add(validationErrorLabel);
        Controls.Add(selectProjectDirectoryButton);
        Controls.Add(projectDirectoryTextBox);
        Controls.Add(projectDirectoryLabel);
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
