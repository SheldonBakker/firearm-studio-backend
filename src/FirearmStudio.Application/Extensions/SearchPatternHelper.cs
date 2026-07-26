namespace FirearmStudio.Application.Extensions;

internal static class SearchPatternHelper
{
    /// <summary>
    /// Escapes backslash, percent, and underscore in <paramref name="term"/> so it is safe
    /// to embed as the literal portion of an ILike pattern, then wraps it in %...% for a
    /// case-insensitive substring search.
    /// </summary>
    internal static string ToILikeContainsPattern(string term)
        => "%" + EscapeILikeLiteral(term) + "%";

    /// <summary>
    /// Escapes backslash, percent, and underscore in <paramref name="term"/> and returns it
    /// as an exact-match ILike pattern (no % wrapping). Use this for case-insensitive
    /// equality checks via EF.Functions.ILike.
    /// </summary>
    internal static string ToILikeExactPattern(string term)
        => EscapeILikeLiteral(term);

    private static string EscapeILikeLiteral(string term)
        => term.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}
