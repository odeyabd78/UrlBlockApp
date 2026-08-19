using System;
using System.Configuration;
using System.Data;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using UrlBlockListWpfApp.Services;

namespace UrlBlockListWpfApp;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private readonly IHost _host;

    public App()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((context, configuration) =>
            {
                configuration.SetBasePath(AppContext.BaseDirectory);

                configuration.AddJsonFile(
                    path: "appsettings.json",
                    optional: false,
                    reloadOnChange: true);
            })
            .ConfigureServices((context, services) =>
            {
                services.AddSingleton<IRegistryFileService, RegistryFileService>();
                services.AddSingleton<IBrowserService, BrowserService>();
                services.AddSingleton<IBrowserPolicyService, BrowserPolicyService>();
                services.AddOptions<BlockListOptions>()
                    .Bind(
                        context.Configuration.GetRequiredSection(
                            nameof(BlockListOptions)))
                    .ValidateOnStart();
                services.AddSingleton<MainWindowViewModel>();
                services.AddSingleton<MainWindow>();
            })
            .Build();
    }

    protected override async void OnStartup(
        StartupEventArgs e)
    {
        await _host.StartAsync();

        MainWindow mainWindow =
            _host.Services.GetRequiredService<MainWindow>();

        mainWindow.Show();

        base.OnStartup(e);
    }

    protected override async void OnExit(
        ExitEventArgs e)
    {
        await _host.StopAsync();
        _host.Dispose();

        base.OnExit(e);
    }
}