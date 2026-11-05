namespace Encurtador.Web.Services;

public enum FormField
{
    DestinationUrl,
    Code,
    Expiration,
    General
}

public abstract record ApiResult<T>
{
    public sealed record Success(T Value) : ApiResult<T>;

    public sealed record Rejected(FormField Field, string Message) : ApiResult<T>;

    public sealed record NotFound : ApiResult<T>;

    public sealed record Unavailable(string Message) : ApiResult<T>;
}
