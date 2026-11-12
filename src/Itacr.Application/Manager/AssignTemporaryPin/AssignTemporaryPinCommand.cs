namespace Itacr.Application.Manager.AssignTemporaryPin;

/// <summary>The temporary PIN the manager gives an employee who asked to reset theirs: 4 digits.</summary>
public sealed record AssignTemporaryPinCommand(string? Pin);
