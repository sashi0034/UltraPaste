using System.Text.RegularExpressions;

namespace UltraPaste;

public static partial class TextTransformService
{
    // Split a string into lowercase words, handling spaces, underscores, hyphens, camelCase, PascalCase, UPPER_SNAKE
    private static List<string> SplitWords(string input)
    {
        // Insert separator before uppercase that follows lowercase/digit (camelCase boundary)
        var s = LowerBeforeUpperRegex().Replace(input, "$1\x00$2");
        // Insert separator before uppercase+lowercase that follows a run of uppercase (e.g. HTTPResponse → HTTP|Response)
        s = UpperRunBeforeTitleRegex().Replace(s, "$1\x00$2");
        // Split by whitespace, underscores, hyphens, dots, null char
        var words = WordSplitRegex().Split(s)
            .Where(w => !string.IsNullOrEmpty(w))
            .Select(w => w.ToLowerInvariant())
            .ToList();
        return words;
    }

    public static string ToSnakeCase(string? input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        return ApplyPerLine(input, line =>
        {
            var words = SplitWords(line);
            return string.Join("_", words);
        });
    }

    public static string ToPascalCase(string? input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        return ApplyPerLine(input, line =>
        {
            var words = SplitWords(line);
            return string.Concat(words.Select(Capitalize));
        });
    }

    public static string ToCamelCase(string? input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        return ApplyPerLine(input, line =>
        {
            var words = SplitWords(line);
            if (words.Count == 0) return line;
            return words[0] + string.Concat(words.Skip(1).Select(Capitalize));
        });
    }

    public static string ToUpperSnakeCase(string? input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        return ApplyPerLine(input, line =>
        {
            var words = SplitWords(line);
            return string.Join("_", words.Select(w => w.ToUpperInvariant()));
        });
    }

    public static string ToKebabCase(string? input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        return ApplyPerLine(input, line =>
        {
            var words = SplitWords(line);
            return string.Join("-", words);
        });
    }

    public static string SortLinesAscending(string? input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        var lines = SplitLines(input);
        lines.Sort(StringComparer.OrdinalIgnoreCase);
        return string.Join(Environment.NewLine, lines);
    }

    public static string SortLinesDescending(string? input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        var lines = SplitLines(input);
        lines.Sort(StringComparer.OrdinalIgnoreCase);
        lines.Reverse();
        return string.Join(Environment.NewLine, lines);
    }

    public static string UniqueLines(string? input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        var seen = new LinkedList<string>();
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in SplitLines(input))
        {
            if (set.Add(line)) seen.AddLast(line);
        }
        return string.Join(Environment.NewLine, seen);
    }

    public static string TrimLines(string? input)
    {
        if (string.IsNullOrEmpty(input)) return "";
        return string.Join(Environment.NewLine, SplitLines(input).Select(l => l.Trim()));
    }

    private static string ApplyPerLine(string input, Func<string, string> transform)
    {
        var lines = SplitLines(input);
        return string.Join(Environment.NewLine, lines.Select(transform));
    }

    private static List<string> SplitLines(string input) =>
        input.Split('\n').Select(l => l.TrimEnd('\r')).ToList();

    private static string Capitalize(string word) =>
        word.Length == 0 ? word : char.ToUpperInvariant(word[0]) + word[1..];

    [GeneratedRegex(@"([a-z0-9])([A-Z])")]
    private static partial Regex LowerBeforeUpperRegex();

    [GeneratedRegex(@"([A-Z]+)([A-Z][a-z])")]
    private static partial Regex UpperRunBeforeTitleRegex();

    [GeneratedRegex(@"[\s\x00_\-\.]+")]
    private static partial Regex WordSplitRegex();
}
