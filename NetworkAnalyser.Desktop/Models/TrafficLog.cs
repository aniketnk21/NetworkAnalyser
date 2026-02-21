using CommunityToolkit.Mvvm.ComponentModel;

namespace NetworkAnalyser.Desktop.Models;

public partial class TrafficLog : ObservableObject
{
    [ObservableProperty] private long _id;
    [ObservableProperty] private string _processName = string.Empty;
    [ObservableProperty] private int _processId;
    [ObservableProperty] private string _remoteAddress = string.Empty;
    [ObservableProperty] private int _remotePort;
    [ObservableProperty] private string _action = string.Empty; // "Connected", "Disconnected", "Data Transfer"
    [ObservableProperty] private long _bytesTransferred;
    [ObservableProperty] private DateTime _timestamp = DateTime.Now;
    [ObservableProperty] private string _details = string.Empty;
    [ObservableProperty] private bool _isSuspicious;
    [ObservableProperty] private string _remoteCountry = string.Empty;
}
