#nullable enable

namespace AuxCodex.Forms;

partial class CompletionHistoryForm
{
    private Label itemLabel = null!;
    private DataGridView historyGrid = null!;
    private Label emptyLabel = null!;
    private Button closeButton = null!;
    private DataGridViewTextBoxColumn dateColumn = null!;
    private DataGridViewTextBoxColumn percentageColumn = null!;
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
        itemLabel = new Label();
        historyGrid = new DataGridView();
        dateColumn = new DataGridViewTextBoxColumn();
        percentageColumn = new DataGridViewTextBoxColumn();
        emptyLabel = new Label();
        closeButton = new Button();
        ((System.ComponentModel.ISupportInitialize)historyGrid).BeginInit();
        SuspendLayout();
        //
        // itemLabel
        //
        itemLabel.AutoSize = true;
        itemLabel.Location = new Point(16, 14);
        itemLabel.Name = "itemLabel";
        itemLabel.Size = new Size(0, 15);
        itemLabel.TabIndex = 0;
        //
        // historyGrid
        //
        historyGrid.AllowUserToAddRows = false;
        historyGrid.AllowUserToDeleteRows = false;
        historyGrid.AllowUserToOrderColumns = false;
        historyGrid.AllowUserToResizeRows = false;
        historyGrid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        historyGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
        historyGrid.BackgroundColor = SystemColors.Window;
        historyGrid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        historyGrid.Columns.AddRange(new DataGridViewColumn[] { dateColumn, percentageColumn });
        historyGrid.Location = new Point(16, 44);
        historyGrid.MultiSelect = false;
        historyGrid.Name = "historyGrid";
        historyGrid.ReadOnly = true;
        historyGrid.RowHeadersVisible = false;
        historyGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        historyGrid.ShowCellToolTips = false;
        historyGrid.Size = new Size(488, 258);
        historyGrid.TabIndex = 2;
        //
        // dateColumn
        //
        dateColumn.HeaderText = "Data/Hora";
        dateColumn.Name = "dateColumn";
        dateColumn.ReadOnly = true;
        dateColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
        dateColumn.Width = 180;
        //
        // percentageColumn
        //
        percentageColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        percentageColumn.HeaderText = "Completude";
        percentageColumn.Name = "percentageColumn";
        percentageColumn.ReadOnly = true;
        percentageColumn.SortMode = DataGridViewColumnSortMode.NotSortable;
        //
        // emptyLabel
        //
        emptyLabel.AutoSize = true;
        emptyLabel.ForeColor = SystemColors.GrayText;
        emptyLabel.Location = new Point(16, 44);
        emptyLabel.Name = "emptyLabel";
        emptyLabel.Size = new Size(0, 15);
        emptyLabel.TabIndex = 3;
        emptyLabel.Text = "Nenhuma alteração de completude registrada.";
        emptyLabel.Visible = false;
        //
        // closeButton
        //
        closeButton.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        closeButton.DialogResult = DialogResult.Cancel;
        closeButton.Location = new Point(419, 314);
        closeButton.Name = "closeButton";
        closeButton.Size = new Size(85, 28);
        closeButton.TabIndex = 1;
        closeButton.Text = "Fechar";
        closeButton.UseVisualStyleBackColor = true;
        //
        // CompletionHistoryForm
        //
        AcceptButton = closeButton;
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        CancelButton = closeButton;
        ClientSize = new Size(520, 360);
        Controls.Add(closeButton);
        Controls.Add(emptyLabel);
        Controls.Add(historyGrid);
        Controls.Add(itemLabel);
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = false;
        MinimizeBox = false;
        MinimumSize = new Size(420, 300);
        Name = "CompletionHistoryForm";
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "Histórico de completude";
        ((System.ComponentModel.ISupportInitialize)historyGrid).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }
}
