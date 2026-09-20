namespace AuxCodex.Services;

using AuxCodex.Models;

/// <summary>
/// Resultado possivel de uma tentativa de movimentacao.
/// </summary>
public enum MoveOutcome
{
    Ready,
    Moved,
    SameLocation,
    DuplicateName,
    Cycle,
    Invalid
}

/// <summary>
/// Resultado de uma movimentacao, incluindo a mensagem amigavel a exibir.
/// </summary>
public sealed class MoveResult
{
    private MoveResult(MoveOutcome outcome, string? errorMessage)
    {
        Outcome = outcome;
        ErrorMessage = errorMessage;
    }

    public MoveOutcome Outcome { get; }

    public string? ErrorMessage { get; }

    public bool Succeeded => Outcome == MoveOutcome.Moved;

    public static MoveResult Moved() => new(MoveOutcome.Moved, null);

    public static MoveResult Invalid(string errorMessage) => new(MoveOutcome.Invalid, errorMessage);

    public static MoveResult FromOutcome(MoveOutcome outcome) => outcome switch
    {
        MoveOutcome.SameLocation => new MoveResult(MoveOutcome.SameLocation, null),
        MoveOutcome.DuplicateName => new MoveResult(
            MoveOutcome.DuplicateName,
            "Já existe uma pasta ou projeto com esse nome neste local."),
        MoveOutcome.Cycle => new MoveResult(
            MoveOutcome.Cycle,
            "Uma pasta não pode ser movida para ela mesma ou para uma de suas subpastas."),
        _ => new MoveResult(MoveOutcome.Invalid, "A movimentação não pode ser concluída.")
    };
}