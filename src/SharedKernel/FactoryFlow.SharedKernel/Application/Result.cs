namespace FactoryFlow.SharedKernel.Application;

public class Result
{
    protected Result(bool succeeded, IReadOnlyList<string> errors)
    {
        Succeeded = succeeded;
        Errors = errors;
    }

    public bool Succeeded { get; }
    public IReadOnlyList<string> Errors { get; }

    public static Result Success() => new(true, []);
    public static Result Failure(params string[] errors) => new(false, errors);
    public static Result<T> Success<T>(T value) => new(value, true, []);
    public static Result<T> Failure<T>(params string[] errors) => new(default, false, errors);
}

public class Result<T> : Result
{
    internal Result(T? value, bool succeeded, IReadOnlyList<string> errors)
        : base(succeeded, errors)
    {
        Value = value;
    }

    public T? Value { get; }
}
