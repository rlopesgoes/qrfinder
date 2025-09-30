namespace Domain.Common;

public enum StatusCode
{
    Success,
    NoContent,
    NotFound,
    Error,
}

public class Result
{
    public StatusCode StatusCode { get; set; }
    public Error? Error { get; set; }
    public List<Validation>? Validations { get; set; }

    public static Result Success() => new()
    {
        StatusCode = StatusCode.Success
    };
    
    public static Result WithError(string message) => new()
    {
        Error = new Error(message),
        StatusCode = StatusCode.Error
    };
    
    public static Result WithError(Exception exception) => new()
    {
        Error = new Error(exception.Message, exception),
        StatusCode = StatusCode.Error
    };
    
    public static implicit operator Result(Exception exception) => WithError(exception);
    public bool IsSuccess => StatusCode is StatusCode.Success;
    public bool IsSuccessOrNoContent => StatusCode is StatusCode.Success or StatusCode.NoContent;
}

public class Result<T> : Result
{
    public T? Value { get; init; }

    public static Result<T> Success(T value)
        => new()
        {
            Value = value,
            StatusCode = StatusCode.Success
        };
    
    public new static Result<T> WithError(string message)
        => new()
        {
            Error = new Error(message),
            StatusCode = StatusCode.Error
        };
    
    public new static Result<T> WithError(Exception exception)
        => new()
        {
            Error = new Error(exception.Message, exception),
            StatusCode = StatusCode.Error
        };

    public static Result<T> EntityNotFound(string message)
        => new()
        {
            StatusCode = StatusCode.NotFound,
            Error = new Error(message)
        };
    
    public static Result<T> NoContent()
        => new()
        {
            StatusCode = StatusCode.NoContent
        };
    
    public static Result<T> FromResult(Result input) =>
        new()
        {
            StatusCode = input.StatusCode,
            Error = input.Error,
            Validations = input.Validations ?? []
        };

    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(Exception exception) => WithError(exception);
    
}

public record Error(string Message, Exception? Exception = null);

public record Validation(string Property, string Error)
{
    public override string ToString() => $"Property: {Property}, Error: {Error}";
}