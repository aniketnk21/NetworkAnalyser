using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NetworkAnalyser.Desktop.Models;
using NetworkAnalyser.Desktop.Services;

namespace NetworkAnalyser.Desktop.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly DatabaseService _db;
    private readonly NetworkMonitorService _monitor;
    private readonly LogCleanupService _cleanup;

    [ObservableProperty] private bool _isMonitoring;
    [ObservableProperty] private int _activeConnectionCount;
    [ObservableProperty] private int _uniqueProcessCount;
    [ObservableProperty] private int _suspiciousCount;
    [ObservableProperty] private long _totalDataTransferred;
    [ObservableProperty] private int _totalLogEntries;
    [ObservableProperty] private string _statusMessage = "Ready to monitor";
    [ObservableProperty] private string _searchFilter = string.Empty;
    [ObservableProperty] private string _lastCleanupInfo = "No cleanup yet";

    public ObservableCollection<NetworkConnection> ActiveConnections { get; } = new();
    public ObservableCollection<TrafficLog> TrafficLogs { get; } = new();
    public ObservableCollection<TrafficLog> SuspiciousAlerts { get; } = new();

    public MainViewModel(DatabaseService db, NetworkMonitorService monitor, LogCleanupService cleanup)
    {
        _db = db;
        _monitor = monitor;
        _cleanup = cleanup;

        _monitor.ConnectionsUpdated += OnConnectionsUpdated;
        _monitor.NewTrafficLog += OnNewTrafficLog;
        _monitor.ErrorOccurred += msg => StatusMessage = $"Error: {msg}";

        _cleanup.LogsCleaned += count =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                LastCleanupInfo = $"Cleaned {count} old entries at {DateTime.Now:HH:mm:ss}";
                StatusMessage = LastCleanupInfo;
            });
        };

        // Load existing logs from DB
        var existingLogs = _db.GetTrafficLogs(200);
        foreach (var log in existingLogs)
        {
            TrafficLogs.Add(log);
            if (log.IsSuspicious) SuspiciousAlerts.Add(log);
        }
    }

    [RelayCommand]
    private void ToggleMonitoring()
    {
        if (IsMonitoring)
        {
            _monitor.Stop();
            IsMonitoring = false;
            StatusMessage = "Monitoring stopped";
        }
        else
        {
            _monitor.Start();
            _cleanup.Start();
            IsMonitoring = true;
            StatusMessage = "Monitoring active — scanning connections every 2s";
        }
    }

    [RelayCommand]
    private void ClearLogs()
    {
        TrafficLogs.Clear();
        SuspiciousAlerts.Clear();
        TotalLogEntries = 0;
        StatusMessage = "Logs cleared from view";
    }

    [RelayCommand]
    private void SearchIpAddress(string? ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress)) return;
        try
        {
            var url = $"https://www.google.com/search?q={Uri.EscapeDataString(ipAddress)}";
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to open browser: {ex.Message}";
        }
    }

    private void OnConnectionsUpdated(List<NetworkConnection> connections)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            ActiveConnections.Clear();

            var filtered = string.IsNullOrWhiteSpace(SearchFilter)
                ? connections
                : connections.Where(c =>
                    c.ProcessName.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase) ||
                    c.RemoteAddress.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase)).ToList();

            foreach (var conn in filtered)
                ActiveConnections.Add(conn);

            ActiveConnectionCount = connections.Count;
            UniqueProcessCount = connections.Select(c => c.ProcessName).Distinct().Count();
            SuspiciousCount = connections.Count(c => c.IsSuspicious) + SuspiciousAlerts.Count;
        });
    }

    private void OnNewTrafficLog(TrafficLog log)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            TrafficLogs.Insert(0, log);
            TotalLogEntries = TrafficLogs.Count;

            if (log.IsSuspicious)
            {
                SuspiciousAlerts.Insert(0, log);
            }

            // Keep UI list manageable
            while (TrafficLogs.Count > 1000)
                TrafficLogs.RemoveAt(TrafficLogs.Count - 1);
            while (SuspiciousAlerts.Count > 200)
                SuspiciousAlerts.RemoveAt(SuspiciousAlerts.Count - 1);
        });
    }

    partial void OnSearchFilterChanged(string value)
    {
        // Re-filter is handled on next poll cycle automatically
        StatusMessage = string.IsNullOrWhiteSpace(value) ? "Showing all connections" : $"Filtering: {value}";
    }

    public void Shutdown()
    {
        _monitor.Stop();
        _cleanup.Dispose();
        _db.Dispose();
    }
}
