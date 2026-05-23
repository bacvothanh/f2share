namespace F2Share.Desktop.ViewModels;

public sealed class TransferItemViewModel : ViewModelBase
{
    private double _progress;
    private string _state = "Queued";

    public required string RelativePath { get; init; }

    public double Progress
    {
        get => _progress;
        set => Set(ref _progress, value);
    }

    public string State
    {
        get => _state;
        set => Set(ref _state, value);
    }
}
