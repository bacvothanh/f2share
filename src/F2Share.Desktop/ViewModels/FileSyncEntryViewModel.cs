namespace F2Share.Desktop.ViewModels;

public sealed class FileSyncEntryViewModel
{
    public required string RelativePath { get; init; }
    public required string SizeLabel { get; init; }
    public required string LastWriteLabel { get; init; }
}
