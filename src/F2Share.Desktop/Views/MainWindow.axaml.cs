using Avalonia.Controls;
using Avalonia.Styling;
using F2Share.Desktop.ViewModels;

namespace F2Share.Desktop.Views;

public partial class MainWindow : Window
{
    private MainViewModel? _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        Closed += OnClosed;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        _viewModel = DataContext as MainViewModel;
        if (_viewModel is null)
        {
            return;
        }

        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        ApplyTheme(_viewModel.IsDarkMode);
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (_viewModel is null || e.PropertyName != nameof(MainViewModel.IsDarkMode))
        {
            return;
        }

        ApplyTheme(_viewModel.IsDarkMode);
    }

    private static void ApplyTheme(bool isDark)
    {
        if (global::Avalonia.Application.Current is null)
        {
            return;
        }

        global::Avalonia.Application.Current.RequestedThemeVariant = isDark ? ThemeVariant.Dark : ThemeVariant.Light;
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        if (_viewModel is null)
        {
            return;
        }

        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
    }
}
