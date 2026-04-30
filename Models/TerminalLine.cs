using CommunityToolkit.Mvvm.ComponentModel;

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

public partial class TerminalLine : ObservableObject
{
    [ObservableProperty] private string _text = string.Empty;
    public TerminalLineType Type      { get; set; } = TerminalLineType.Output;
    public DateTime         Timestamp { get; set; } = DateTime.Now;
}
