using System.Diagnostics;
using System.Runtime.InteropServices;
using NetworkAnalyser.Desktop.Helpers;
using NetworkAnalyser.Desktop.Models;

namespace NetworkAnalyser.Desktop.Services;

/// <summary>
/// Monitors active TCP connections by polling the Windows extended TCP table.
/// Maps connections to owning processes and detects suspicious activity.
/// </summary>
public class NetworkMonitorService : IDisposable
{
    private Timer? _pollTimer;
    private readonly DatabaseService _db;
    private readonly Dictionary<string, NetworkConnection> _previousConnections = new();
    private bool _isMonitoring;

    // Well-known safe ports
    private static readonly HashSet<int> SafePorts = new()
    {
        80, 443, 53, 993, 995, 587, 465, 143, 110, 25, 8080, 8443
    };

    // Suspicious ports commonly used by malware
    private static readonly HashSet<int> SuspiciousPorts = new()
    {
        4444, 5555, 6666, 7777, 8888, 9999,  // Common reverse shells
        1337, 31337,                           // Hacker lore
        3389,                                  // RDP (if unexpected)
        4443, 8443,                            // Alt HTTPS
        6667, 6697,                            // IRC (C2 channels)
        1080, 9050, 9051                       // SOCKS/Tor
    };

    public event Action<List<NetworkConnection>>? ConnectionsUpdated;
    public event Action<TrafficLog>? NewTrafficLog;
    public event Action<string>? ErrorOccurred;

    public bool IsMonitoring => _isMonitoring;

    public NetworkMonitorService(DatabaseService db)
    {
        _db = db;
    }

    public void Start(int pollIntervalMs = 2000)
    {
        if (_isMonitoring) return;
        _isMonitoring = true;
        _pollTimer = new Timer(PollConnections, null, 0, pollIntervalMs);
    }

    public void Stop()
    {
        _isMonitoring = false;
        _pollTimer?.Change(Timeout.Infinite, Timeout.Infinite);
        _pollTimer?.Dispose();
        _pollTimer = null;
    }

    private void PollConnections(object? state)
    {
        try
        {
            var connections = GetTcpConnections();
            var currentKeys = new HashSet<string>();

            foreach (var conn in connections)
            {
                var key = $"{conn.ProcessId}_{conn.RemoteAddress}:{conn.RemotePort}_{conn.State}";
                currentKeys.Add(key);

                // Detect new connections
                if (!_previousConnections.ContainsKey(key))
                {
                    // Analyze for suspicious activity
                    AnalyzeSuspicious(conn);

                    // Log to DB
                    _db.InsertConnection(conn);

                    var log = new TrafficLog
                    {
                        ProcessName = conn.ProcessName,
                        ProcessId = conn.ProcessId,
                        RemoteAddress = conn.RemoteAddress,
                        RemotePort = conn.RemotePort,
                        Action = conn.State == "ESTABLISHED" ? "Connected" : conn.State,
                        Timestamp = DateTime.Now,
                        Details = $"{conn.ProcessName} → {conn.RemoteAddress}:{conn.RemotePort} [{conn.State}]",
                        IsSuspicious = conn.IsSuspicious
                    };
                    _db.InsertTrafficLog(log);
                    NewTrafficLog?.Invoke(log);
                }
            }

            // Detect disconnected connections
            foreach (var kvp in _previousConnections)
            {
                if (!currentKeys.Contains(kvp.Key))
                {
                    var conn = kvp.Value;
                    var log = new TrafficLog
                    {
                        ProcessName = conn.ProcessName,
                        ProcessId = conn.ProcessId,
                        RemoteAddress = conn.RemoteAddress,
                        RemotePort = conn.RemotePort,
                        Action = "Disconnected",
                        Timestamp = DateTime.Now,
                        Details = $"{conn.ProcessName} disconnected from {conn.RemoteAddress}:{conn.RemotePort}",
                        IsSuspicious = conn.IsSuspicious
                    };
                    _db.InsertTrafficLog(log);
                    NewTrafficLog?.Invoke(log);
                }
            }

            // Update previous state
            _previousConnections.Clear();
            foreach (var conn in connections)
            {
                var key = $"{conn.ProcessId}_{conn.RemoteAddress}:{conn.RemotePort}_{conn.State}";
                _previousConnections[key] = conn;
            }

            ConnectionsUpdated?.Invoke(connections);
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(ex.Message);
        }
    }

    private void AnalyzeSuspicious(NetworkConnection conn)
    {
        var reasons = new List<string>();

        // Check for suspicious remote ports
        if (SuspiciousPorts.Contains(conn.RemotePort))
        {
            reasons.Add($"Suspicious port {conn.RemotePort}");
        }

        // Check for non-standard ports (not in safe list) on established connections
        if (conn.State == "ESTABLISHED" && !SafePorts.Contains(conn.RemotePort) && conn.RemotePort < 1024)
        {
            reasons.Add($"Unusual well-known port {conn.RemotePort}");
        }

        // Check for unknown/unusual process names
        var suspiciousProcessPatterns = new[] { "cmd.exe", "powershell.exe", "wscript.exe",
            "cscript.exe", "mshta.exe", "regsvr32.exe", "rundll32.exe", "certutil.exe" };
        if (suspiciousProcessPatterns.Any(p => conn.ProcessName.Equals(p, StringComparison.OrdinalIgnoreCase)))
        {
            reasons.Add($"Suspicious process: {conn.ProcessName}");
        }

        // Check for connections to private IP ranges from unexpected processes
        if (conn.RemoteAddress.StartsWith("10.") || conn.RemoteAddress.StartsWith("192.168.") ||
            conn.RemoteAddress.StartsWith("172."))
        {
            // Internal network connections from scripting engines are suspicious
            if (suspiciousProcessPatterns.Any(p => conn.ProcessName.Equals(p, StringComparison.OrdinalIgnoreCase)))
            {
                reasons.Add("Internal network connection from scripting engine");
            }
        }

        if (reasons.Count > 0)
        {
            conn.IsSuspicious = true;
            conn.SuspiciousReason = string.Join("; ", reasons);
        }
    }

    private List<NetworkConnection> GetTcpConnections()
    {
        var connections = new List<NetworkConnection>();

        int bufferSize = 0;
        // First call to get buffer size
        NativeMethods.GetExtendedTcpTable(IntPtr.Zero, ref bufferSize, true,
            NativeMethods.AF_INET, NativeMethods.TCP_TABLE_OWNER_PID_ALL);

        var tcpTablePtr = Marshal.AllocHGlobal(bufferSize);
        try
        {
            uint result = NativeMethods.GetExtendedTcpTable(tcpTablePtr, ref bufferSize, true,
                NativeMethods.AF_INET, NativeMethods.TCP_TABLE_OWNER_PID_ALL);

            if (result != 0) return connections;

            var table = Marshal.PtrToStructure<NativeMethods.MIB_TCPTABLE_OWNER_PID>(tcpTablePtr);
            int rowSize = Marshal.SizeOf<NativeMethods.MIB_TCPROW_OWNER_PID>();
            IntPtr rowPtr = tcpTablePtr + Marshal.SizeOf<NativeMethods.MIB_TCPTABLE_OWNER_PID>();

            for (int i = 0; i < table.dwNumEntries; i++)
            {
                var row = Marshal.PtrToStructure<NativeMethods.MIB_TCPROW_OWNER_PID>(rowPtr);

                string processName = "Unknown";
                try
                {
                    var proc = Process.GetProcessById(row.owningPid);
                    processName = proc.ProcessName;
                }
                catch
                {
                    // Process may have exited
                }

                // Skip loopback connections on LISTEN
                var remoteAddr = row.RemoteAddress.ToString();
                if (remoteAddr == "0.0.0.0" && row.StateString == "LISTEN")
                {
                    rowPtr += rowSize;
                    continue;
                }

                connections.Add(new NetworkConnection
                {
                    ProcessId = row.owningPid,
                    ProcessName = processName,
                    LocalAddress = row.LocalAddress.ToString(),
                    LocalPort = row.LocalPort,
                    RemoteAddress = remoteAddr,
                    RemotePort = row.RemotePort,
                    State = row.StateString,
                    Protocol = "TCP",
                    Timestamp = DateTime.Now
                });

                rowPtr += rowSize;
            }
        }
        finally
        {
            Marshal.FreeHGlobal(tcpTablePtr);
        }

        return connections;
    }

    public void Dispose()
    {
        Stop();
    }
}
