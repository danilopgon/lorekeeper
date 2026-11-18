namespace Api.SharedKernel;

public enum ErrorKind
{
    Validation,
    NotFound,
    Conflict
}

public sealed record FieldError(string Field, string Code);

/// <summary>An expected failure with a stable public code; Presentation maps <see cref="Kind"/> to the HTTP status.</summary>
public sealed record Error(string Code, ErrorKind Kind, string Message, IReadOnlyList<FieldError> Fields)
{
    public static Error Validation(string code, string message) => new(code, ErrorKind.Validation, message, []);

    public static Error NotFound(string code, string message) => new(code, ErrorKind.NotFound, message, []);

    public static Error Conflict(string code, string message) => new(code, ErrorKind.Conflict, message, []);

    /// <summary>Several invalid fields; the first one's code becomes the error code.</summary>
    public static Error InvalidFields(string message, IReadOnlyList<FieldError> fields) =>
        new(fields[0].Code, ErrorKind.Validation, message, fields);

    public Error ForField(string field) => this with { Fields = [new FieldError(field, Code)] };

    public bool Equals(Error? other) =>
        other is not null
        && Code == other.Code
        && Kind == other.Kind
        && Message == other.Message
        && Fields.SequenceEqual(other.Fields);

    public override int GetHashCode() => HashCode.Combine(Code, Kind, Message, Fields.Count);
}

public sealed class Result<T>
{
    private readonly T? _value;

    private Result(T? value, Error? error)
    {
        _value = value;
        Error = error;
    }

    public bool IsSuccess => Error is null;

    public Error? Error { get; }

    public T Value => IsSuccess ? _value! : throw new InvalidOperationException($"A failed result has no value: {Error!.Code}.");

    public static Result<T> Success(T value) => new(value, null);

    public static Result<T> Failure(Error error) => new(default, error);

    public static implicit operator Result<T>(T value) => Success(value);

    public static implicit operator Result<T>(Error error) => Failure(error);

    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<Error, TOut> onFailure) =>
        IsSuccess ? onSuccess(_value!) : onFailure(Error!);
}
