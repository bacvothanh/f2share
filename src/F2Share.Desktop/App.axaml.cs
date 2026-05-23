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

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var rootPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "F2Share");
        Directory.CreateDirectory(rootPath);

        var deviceId = Environment.MachineName + "-" + Guid.NewGuid().ToString("N")[..6];
        var shareId = "default-share";
        var dbPath = Path.Combine(rootPath, ".f2share", "metadata.db");

        _host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddF2ShareApplication();
                services.AddF2ShareInfrastructure(shareId, rootPath, dbPath, deviceId, Environment.MachineName, 40177);
                services.AddF2ShareTransport(deviceId);
                services.AddSingleton<MainViewModel>();
            })
            .Build();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = _host.Services.GetRequiredService<MainViewModel>()
            };
        }

        _host.Start();
        base.OnFrameworkInitializationCompleted();
    }
}
