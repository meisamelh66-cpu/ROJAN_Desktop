namespace Rojan.Desktop.Application.Organizations;

/// <summary>Thrown by <see cref="IPermissionGate"/> when the current session's role lacks a required <see cref="Permission"/> - the concrete "unauthorized operations must never execute" enforcement every mutating command service now depends on.</summary>
public sealed class UnauthorizedOperationException : Exception
{
    public UnauthorizedOperationException(Permission requiredPermission)
        : base($"The current role does not have the '{requiredPermission}' permission required for this operation.")
    {
        RequiredPermission = requiredPermission;
    }

    public UnauthorizedOperationException()
    {
    }

    public UnauthorizedOperationException(string message)
        : base(message)
    {
    }

    public UnauthorizedOperationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public Permission RequiredPermission { get; }
}
