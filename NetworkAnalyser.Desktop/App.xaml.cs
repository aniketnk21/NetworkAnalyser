using System.Windows;
using NetworkAnalyser.Desktop.Services;
using NetworkAnalyser.Desktop.ViewModels;

namespace NetworkAnalyser.Desktop;

public partial class App : Application
{
    private DatabaseService? _db;
    private NetworkMonitorService? _monitor;
    private LogCleanupService? _cleanup;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Initialize services
        _db = new DatabaseService();
        _db.Initialize();

        _monitor = new NetworkMonitorService(_db);
        _cleanup = new LogCleanupService(_db);

        // Create ViewModel and MainWindow
        var viewModel = new MainViewModel(_db, _monitor, _cleanup);
        var mainWindow = new MainWindow(viewModel);
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _monitor?.Dispose();
        _cleanup?.Dispose();
        _db?.Dispose();
        base.OnExit(e);
    }
}
