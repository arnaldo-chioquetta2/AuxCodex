using System.Globalization;
using AuxCodex.Models;

namespace AuxCodex.Forms;

/// <summary>
/// MCO:63 - Consulta somente leitura do historico de completude de um Item.
/// A janela trabalha sobre um snapshot; nunca altera a colecao persistida.
/// </summary>
public partial class CompletionHistoryForm : Form
{
    public CompletionHistoryForm(string itemName, IEnumerable<CompletionHistoryEntry>? history)
    {
        InitializeComponent();
        Icon = Utils.ApplicationIconProvider.Icon;
        itemLabel.Text = $"Item: {(string.IsNullOrWhiteSpace(itemName) ? "(novo item)" : itemName.Trim())}";
        LoadHistory(history);
    }

    private void LoadHistory(IEnumerable<CompletionHistoryEntry>? history)
    {
        // Copia defensiva: a colecao do Item permanece intacta e na ordem persistida.
        // A inversao cronologica abaixo e apenas visual (mais recente primeiro).
        var entries = (history ?? Array.Empty<CompletionHistoryEntry>())
            .Select(entry => new CompletionHistoryEntry
            {
                ChangedAt = entry.ChangedAt,
                Percentage = Math.Clamp(entry.Percentage, 0, 100)
            })
            .OrderByDescending(entry => entry.ChangedAt)
            .ToList();

        historyGrid.Rows.Clear();
        foreach (var entry in entries)
        {
            historyGrid.Rows.Add(
                entry.ChangedAt.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture),
                $"{entry.Percentage}%");
        }

        var hasEntries = entries.Count > 0;
        historyGrid.Visible = hasEntries;
        emptyLabel.Visible = !hasEntries;
    }
}
