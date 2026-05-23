using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using F2Share.Application;
using F2Share.Desktop.ViewModels;
using F2Share.Desktop.Views;
using F2Share.Infrastructure;
using F2Share.Transport;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace F2Share.Desktop;

public partial class App : global::Avalonia.Application
{
    private IHost? _host;
    private string _shareRootPath = string.Empty;
    private string _shareId = "default-share";

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        _shareRootPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "F2Share");
        Directory.CreateDirectory(_shareRootPath);

        var deviceId = Environment.MachineName + "-" + Guid.NewGuid().ToString("N")[..6];
        var dbPath = Path.Combine(_shareRootPath, ".f2share", "metadata.db");

        _host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddF2ShareApplication();
                services.AddF2ShareInfrastructure(_shareId, _shareRootPath, dbPath, deviceId, Environment.MachineName, 40177);
                services.AddF2ShareTransport(deviceId);
                services.AddSingleton<MainViewModel>();
            })
            .Build();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Exit += OnDesktopExit;
            desktop.MainWindow = new MainWindow
            {
                DataContext = _host.Services.GetRequiredService<MainViewModel>()
            };
        }

        _host.Start();
        base.OnFrameworkInitializationCompleted();
    }

    private async void OnDesktopExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        if (_host is null)
        {
            return;
        }

        await _host.StopAsync(TimeSpan.FromSeconds(5));
        _host.Dispose();
    }
}
