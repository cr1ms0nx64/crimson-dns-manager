using CrimsonDnsManager.App.Data;
using CrimsonDnsManager.App.Services;
using CrimsonDnsManager.App.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using System;
using System.IO;
using System.Threading.Tasks;

namespace CrimsonDnsManager.App;

public partial class App : Application
{
    public static IHost Host { get; private set; } = null!;
    private Window? _window;

    private static readonly string LogPath =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CrimsonDnsManager", "startup.log");

    public App()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
        File.AppendAllText(LogPath, $"App constructor reached: {DateTime.Now:O}{Environment.NewLine}");

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            File.AppendAllText(LogPath, $"UnhandledException: {e.ExceptionObject}{Environment.NewLine}");
        };

        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            File.AppendAllText(LogPath, $"UnobservedTaskException: {e.Exception}{Environment.NewLine}");
        };

        InitializeComponent();
        File.AppendAllText(LogPath, $"App XAML initialized: {DateTime.Now:O}{Environment.NewLine}");

        Host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
            .ConfigureServices((_, services) =>
            {
                services.AddDbContext<AppDbContext>(options =>
                    options.UseSqlite($"Data Source={AppPaths.DatabasePath}"), ServiceLifetime.Singleton);

                services.AddHttpClient<CloudflareApiService>(client =>
                {
                    client.BaseAddress = new Uri("https://api.cloudflare.com/client/v4/");
                    client.Timeout = TimeSpan.FromSeconds(30);
                });

                services.AddSingleton<ISecretService, DpapiSecretService>();
                services.AddSingleton<WindowHandleProvider>();
                services.AddSingleton<LivefeedService>();
                services.AddSingleton<SettingsService>();
                services.AddSingleton<DnsRecordService>();
                services.AddSingleton<UpdateService>();

                services.AddSingleton<ShellViewModel>();
                services.AddSingleton<CloudflareSettingsViewModel>();
                services.AddSingleton<DnsRecordsViewModel>();
                services.AddSingleton<UpdateViewModel>();
            })
            .Build();

        File.AppendAllText(LogPath, $"Host built: {DateTime.Now:O}{Environment.NewLine}");
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            File.AppendAllText(LogPath, $"OnLaunched reached: {DateTime.Now:O}{Environment.NewLine}");

            InitializeDatabaseBeforeUi();

            _window = new MainWindow();
            _window.Activate();

            File.AppendAllText(LogPath, $"Window activated: {DateTime.Now:O}{Environment.NewLine}");
        }
        catch (Exception ex)
        {
            File.AppendAllText(LogPath, $"Launch exception: {ex}{Environment.NewLine}");
            throw;
        }
    }

    private static void InitializeDatabaseBeforeUi()
    {
        try
        {
            AppPaths.EnsureCreated();

            using var scope = Host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            db.Database.EnsureCreated();

            File.AppendAllText(LogPath, $"Database ensured: {DateTime.Now:O}{Environment.NewLine}");
        }
        catch (Exception ex)
        {
            File.AppendAllText(LogPath, $"Database init exception: {ex}{Environment.NewLine}");
        }
    }

    public static T GetService<T>() where T : notnull => Host.Services.GetRequiredService<T>();
}
