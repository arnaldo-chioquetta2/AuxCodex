namespace AuxCodex.Models;

/// <summary>
/// MCO:62 - Entrada do historico de completude de um Item. Registra somente
/// mudancas efetivas de percentual (0 a 100). Nao guarda nenhum dado sensivel.
/// </summary>
public sealed class CompletionHistoryEntry
{
    public DateTime ChangedAt { get; set; }

    public int Percentage { get; set; }
}
