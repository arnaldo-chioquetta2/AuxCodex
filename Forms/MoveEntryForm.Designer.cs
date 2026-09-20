#nullable enable

namespace AuxCodex.Forms;

partial class MoveEntryForm
{
    private Label formLabel = null!;
    private TreeView destinationTreeView = null!;
    private Button cancelButton = null!;
    private Button moveButton = null!;

    private System.ComponentModel.IContainer? components;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        formLabel = new Label();
        destinationTreeView = new TreeView();
        cancelButton = new Button();
        moveButton = new Button();
        SuspendLayout();
        //
        // formLabel
        //
        formLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        formLabel.Location = new Point(18, 18);
        formLabel.Name = "formLabel";
        formLabel.Size = new Size(484, 22);
        formLabel.TabIndex = 0;
        formLabel.Text = "Mover para:";
        //
        // destinationTreeView
        //
        destinationTreeView.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        destinationTreeView.FullRowSelect = true;
        destinationTreeView.HideSelection = false;
        destinationTreeView.Location = new Point(18, 48);
        destinationTreeView.Name = "destinationTreeView";
        destinationTreeView.Size = new Size(484, 476);
        destinationTreeView.TabIndex = 1;
        //
        // cancelButton
        //
        cancelButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        cancelButton.DialogResult = DialogResult.Cancel;
        cancelButton.Location = new Point(316, 538);
        cancelButton.Name = "cancelButton";
        cancelButton.Size = new Size(85, 28);
        cancelButton.TabIndex = 2;
        cancelButton.Text = "Cancelar";
        cancelButton.UseVisualStyleBackColor = true;
        //
        // moveButton
        //
        moveButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        moveButton.Location = new Point(406, 538);
        moveButton.Name = "moveButton";
        moveButton.Size = new Size(85, 28);
        moveButton.TabIndex = 3;
        moveButton.Text = "Mover";
        moveButton.UseVisualStyleBackColor = true;
        moveButton.Click += OnMoveButtonClick;
        //
        // MoveEntryForm
        //
        AcceptButton = moveButton;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = cancelButton;
        ClientSize = new Size(520, 584);
        Controls.Add(moveButton);
        Controls.Add(cancelButton);
        Controls.Add(destinationTreeView);
        Controls.Add(formLabel);
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = false;
        MinimizeBox = false;
        MinimumSize = new Size(420, 420);
        Name = "MoveEntryForm";
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterScreen;
        Text = "Mover";
        ResumeLayout(false);
    }
}