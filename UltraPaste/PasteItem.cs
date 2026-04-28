namespace UltraPaste;

public class PasteItem
{
    public required string Label { get; init; }
    public required Func<string?, string> GetResult { get; init; }
    public string Preview { get; set; } = "";

    public void RefreshPreview(string? clipboardText)
    {
        try
        {
            var result = GetResult(clipboardText);
            if (string.IsNullOrEmpty(result))
            {
                Preview = "(empty)";
                return;
            }
            var firstLine = result.Split('\n')[0].Trim('\r');
            Preview = firstLine.Length > 50 ? firstLine[..47] + "..." : firstLine;
        }
        catch
        {
            Preview = "";
        }
    }
}
