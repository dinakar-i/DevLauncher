namespace DevLauncher.Models;

public enum TerminalLineType
{
    Command,
    Output,
    Success,
    Error,
    Warning,
    Info,
    Separator
}

public class TerminalLine
{
    public string Text { get; set; } = string.Empty;
    public TerminalLineType Type { get; set; } = TerminalLineType.Output;
    public DateTime Timestamp { get; set; } = DateTime.Now;
}
