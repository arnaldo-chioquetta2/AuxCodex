namespace AuxCodex.Models;

/// <summary>
/// Representa um destino selecionavel na arvore da janela de movimentacao.
/// Um identificador de pasta nulo identifica a raiz da configuracao.
/// </summary>
public sealed class MoveDestinationNode
{
    public MoveDestinationNode(Guid? folderId, string label)
    {
        FolderId = folderId;
        Label = label;
    }

    /// <summary>
    /// Obtem o identificador da pasta, ou <c>null</c> quando o no representa a raiz.
    /// </summary>
    public Guid? FolderId { get; }

    public string Label { get; }

    public bool IsRoot => FolderId is null;

    public List<MoveDestinationNode> Children { get; } = new();
}