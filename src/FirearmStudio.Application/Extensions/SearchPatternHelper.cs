namespace FirearmStudio.Application.Extensions;

internal static class SearchPatternHelper
{
    internal static string ToILikeContainsPattern(string term)
        => "%" + EscapeILikeLiteral(term) + "%";

    internal static string ToILikeExactPattern(string term)
        => EscapeILikeLiteral(term);

    private static string EscapeILikeLiteral(string term)
        => term.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
}
