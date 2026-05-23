namespace F2Share.Desktop.ViewModels;

public sealed class LogEntryViewModel
{
    public required DateTimeOffset TimestampUtc { get; init; }
    public required string Level { get; init; }
    public required string Message { get; init; }

    public string Display => $"[{TimestampUtc:HH:mm:ss}] {Level}: {Message}";
}
