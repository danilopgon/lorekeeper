namespace Api.Modules.Campaigns.Domain;

public sealed record DomainResult<T>(T? Value, string? ErrorCode)
{
    public bool IsSuccess => ErrorCode is null;

    public static DomainResult<T> Success(T value) => new(value, null);

    public static DomainResult<T> Failure(string errorCode) => new(default, errorCode);
}
