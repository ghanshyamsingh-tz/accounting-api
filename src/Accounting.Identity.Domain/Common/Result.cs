using System.Diagnostics.CodeAnalysis;

namespace Accounting.Identity.Domain.Common;

/// <summary>
/// Represents the result of an operation that does not return a value.
/// Implements the Result pattern to avoid exceptions for business rule violations.
/// </summary>
public class Result
{
    protected Result(bool isSuccess, Error? error)
    {
        if (isSuccess && error != null)
        {
            throw new ArgumentException("Successful result cannot have an error.", nameof(error));
        }

        if (!isSuccess && error == null)
        {
            throw new ArgumentException("Failed result must have an error.", nameof(error));
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    
    public bool IsFailure => !IsSuccess;
    
    public Error? Error { get; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static Result Success() => new(true, null);

    /// <summary>
    /// Creates a failed result with the specified error.
    /// </summary>
    public static Result Failure(Error error) => new(false, error);

    /// <summary>
    /// Creates a failed result with the specified error code and message.
    /// </summary>
    public static Result Failure(string code, string message) => 
        new(false, new Error(code, message));
}

/// <summary>
/// Represents the result of an operation that returns a value.
/// Implements the Result pattern to avoid exceptions for business rule violations.
/// </summary>
/// <typeparam name="TValue">The type of the value returned on success.</typeparam>
public class Result<TValue> : Result
{
    private readonly TValue? _value;

    protected Result(TValue value) : base(true, null)
    {
        _value = value;
    }

    protected Result(Error error) : base(false, error)
    {
        _value = default;
    }

    [MemberNotNullWhen(true, nameof(_value))]
    public bool HasValue => IsSuccess;

    /// <summary>
    /// Gets the value if the result is successful.
    /// Throws InvalidOperationException if the result is a failure.
    /// </summary>
    public TValue Value
    {
        get
        {
            if (!IsSuccess)
            {
                throw new InvalidOperationException("Cannot access Value of a failed result. Check IsSuccess first.");
            }

            return _value!;
        }
    }

    /// <summary>
    /// Creates a successful result with the specified value.
    /// </summary>
    public static Result<TValue> Success(TValue value) => new(value);

    /// <summary>
    /// Creates a failed result with the specified error.
    /// </summary>
    public new static Result<TValue> Failure(Error error) => new(error);

    /// <summary>
    /// Creates a failed result with the specified error code and message.
    /// </summary>
    public new static Result<TValue> Failure(string code, string message) => 
        new(new Error(code, message));

    /// <summary>
    /// Implicitly converts a value to a successful Result.
    /// </summary>
    public static implicit operator Result<TValue>(TValue value) => Success(value);

    /// <summary>
    /// Implicitly converts an Error to a failed Result.
    /// </summary>
    public static implicit operator Result<TValue>(Error error) => Failure(error);
}

/// <summary>
/// Represents an error with a code and message.
/// </summary>
public sealed record Error(string Code, string Message)
{
    /// <summary>
    /// Represents no error (success state).
    /// </summary>
    public static readonly Error None = new(string.Empty, string.Empty);

    /// <summary>
    /// Represents a null value error.
    /// </summary>
    public static readonly Error NullValue = new("Error.NullValue", "The specified value cannot be null");
}
