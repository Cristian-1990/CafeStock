namespace CafeStock.Back.Errors.Common;

public abstract record DomainError
{
    public string Message { get; init; } = string.Empty;
    
    protected DomainError(string message)
    {
        Message = message;
    }
}