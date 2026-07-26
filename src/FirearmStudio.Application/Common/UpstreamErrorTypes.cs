namespace FirearmStudio.Application.Common;

/// <summary>
/// Custom ErrorOr numeric type values for errors that don't map to a built-in <see cref="ErrorOr.ErrorType"/>.
/// </summary>
public static class UpstreamErrorTypes
{
    /// <summary>
    /// An external upstream service (e.g. Sage Accounting) failed or was unreachable.
    /// Maps to HTTP 502 Bad Gateway, distinct from generic internal failures (500).
    /// </summary>
    public const int UpstreamFailure = 100;
}
