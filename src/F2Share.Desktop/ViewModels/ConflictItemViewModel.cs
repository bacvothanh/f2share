namespace F2Share.Desktop.ViewModels;

public sealed class ConflictItemViewModel
{
    public required string RelativePath { get; init; }
    public required string DeviceHint { get; init; }
    public required DateTimeOffset DetectedAtUtc { get; init; }
}
