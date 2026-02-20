using CommunityToolkit.Mvvm.ComponentModel;

namespace NetworkAnalyser.Desktop.Models;

public partial class NetworkConnection : ObservableObject
{
    [ObservableProperty] private long _id;
    [ObservableProperty] private int _processId;
    [ObservableProperty] private string _processName = string.Empty;
    [ObservableProperty] private string _localAddress = string.Empty;
    [ObservableProperty] private int _localPort;
    [ObservableProperty] private string _remoteAddress = string.Empty;
    [ObservableProperty] private int _remotePort;
    [ObservableProperty] private string _state = string.Empty;
    [ObservableProperty] private string _protocol = "TCP";
    [ObservableProperty] private long _dataSent;
    [ObservableProperty] private long _dataReceived;
    [ObservableProperty] private DateTime _timestamp = DateTime.Now;
    [ObservableProperty] private bool _isSuspicious;
    [ObservableProperty] private string _suspiciousReason = string.Empty;

    public string RemoteEndpoint => $"{RemoteAddress}:{RemotePort}";
    public string LocalEndpoint => $"{LocalAddress}:{LocalPort}";
}
