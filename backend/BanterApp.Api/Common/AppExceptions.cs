namespace BanterApp.Api.Common;

public abstract class AppException : Exception
{
    protected AppException(
        string code,
        string safeMessage,
        int statusCode,
        string? message = null,
        IReadOnlyDictionary<string, string[]>? details = null,
        bool isRetryable = false)
        : base(message ?? safeMessage)
    {
        Code = code;
        SafeMessage = safeMessage;
        StatusCode = statusCode;
        Details = details;
        IsRetryable = isRetryable;
    }

    public string Code { get; }
    public string SafeMessage { get; }
    public int StatusCode { get; }
    public IReadOnlyDictionary<string, string[]>? Details { get; }
    public bool IsRetryable { get; }
}

public sealed class ValidationAppException : AppException
{
    public ValidationAppException(IReadOnlyDictionary<string, string[]> details)
        : base(ErrorCodes.ValidationError, "Please check the submitted fields.", StatusCodes.Status400BadRequest, details: details)
    {
    }
}

public sealed class NotFoundAppException : AppException
{
    public NotFoundAppException(string safeMessage = "The requested resource was not found.")
        : base(ErrorCodes.NotFound, safeMessage, StatusCodes.Status404NotFound)
    {
    }
}

public sealed class ForbiddenAppException : AppException
{
    public ForbiddenAppException(string safeMessage = "You do not have permission to perform this action.")
        : base(ErrorCodes.Forbidden, safeMessage, StatusCodes.Status403Forbidden)
    {
    }
}

public sealed class UnauthorizedAppException : AppException
{
    public UnauthorizedAppException(string safeMessage = "Authentication is required.")
        : base(ErrorCodes.AuthenticationRequired, safeMessage, StatusCodes.Status401Unauthorized)
    {
    }
}

public sealed class ConflictAppException : AppException
{
    public ConflictAppException(string safeMessage)
        : base(ErrorCodes.Conflict, safeMessage, StatusCodes.Status409Conflict)
    {
    }
}

public sealed class RateLimitedAppException : AppException
{
    public RateLimitedAppException(string safeMessage, int? retryAfterSeconds = null)
        : base(ErrorCodes.RateLimited, safeMessage, StatusCodes.Status429TooManyRequests, isRetryable: true)
    {
        RetryAfterSeconds = retryAfterSeconds;
    }

    public int? RetryAfterSeconds { get; }
}

public sealed class ProviderAppException : AppException
{
    public ProviderAppException(
        string code,
        string safeMessage,
        int statusCode,
        string provider,
        bool isRetryable = false,
        string? operation = null,
        string? providerRequestId = null,
        IReadOnlyDictionary<string, object?>? metadata = null)
        : base(code, safeMessage, statusCode, isRetryable: isRetryable)
    {
        Provider = provider;
        Operation = operation;
        ProviderRequestId = providerRequestId;
        Metadata = metadata;
    }

    public string Provider { get; }
    public string? Operation { get; }
    public string? ProviderRequestId { get; }
    public IReadOnlyDictionary<string, object?>? Metadata { get; }
}
