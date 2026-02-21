using System.IO;
using Microsoft.Data.Sqlite;
using NetworkAnalyser.Desktop.Models;

namespace NetworkAnalyser.Desktop.Services;

/// <summary>
/// Manages SQLite database for storing network connections and traffic logs.
/// </summary>
public class DatabaseService : IDisposable
{
    private readonly string _dbPath;
    private SqliteConnection? _connection;

    public DatabaseService()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NetworkAnalyser");
        Directory.CreateDirectory(appDataPath);
        _dbPath = Path.Combine(appDataPath, "network_logs.db");
    }

    public void Initialize()
    {
        _connection = new SqliteConnection($"Data Source={_dbPath}");
        _connection.Open();

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS Connections (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ProcessId INTEGER NOT NULL,
                ProcessName TEXT NOT NULL,
                LocalAddress TEXT NOT NULL,
                LocalPort INTEGER NOT NULL,
                RemoteAddress TEXT NOT NULL,
                RemotePort INTEGER NOT NULL,
                State TEXT NOT NULL,
                Protocol TEXT NOT NULL DEFAULT 'TCP',
                DataSent INTEGER DEFAULT 0,
                DataReceived INTEGER DEFAULT 0,
                Timestamp TEXT NOT NULL,
                IsSuspicious INTEGER DEFAULT 0,
                SuspiciousReason TEXT DEFAULT ''
            );

            CREATE TABLE IF NOT EXISTS TrafficLogs (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                ProcessName TEXT NOT NULL,
                ProcessId INTEGER NOT NULL,
                RemoteAddress TEXT NOT NULL,
                RemotePort INTEGER NOT NULL,
                Action TEXT NOT NULL,
                BytesTransferred INTEGER DEFAULT 0,
                Timestamp TEXT NOT NULL,
                Details TEXT DEFAULT '',
                IsSuspicious INTEGER DEFAULT 0
            );

            CREATE INDEX IF NOT EXISTS idx_connections_timestamp ON Connections(Timestamp);
            CREATE INDEX IF NOT EXISTS idx_trafficlogs_timestamp ON TrafficLogs(Timestamp);
        ";
        cmd.ExecuteNonQuery();
    }

    public void InsertConnection(NetworkConnection conn)
    {
        if (_connection == null) return;

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO Connections (ProcessId, ProcessName, LocalAddress, LocalPort, RemoteAddress, RemotePort, State, Protocol, DataSent, DataReceived, Timestamp, IsSuspicious, SuspiciousReason)
            VALUES (@pid, @pname, @laddr, @lport, @raddr, @rport, @state, @proto, @dsent, @drecv, @ts, @susp, @sreason)";
        cmd.Parameters.AddWithValue("@pid", conn.ProcessId);
        cmd.Parameters.AddWithValue("@pname", conn.ProcessName);
        cmd.Parameters.AddWithValue("@laddr", conn.LocalAddress);
        cmd.Parameters.AddWithValue("@lport", conn.LocalPort);
        cmd.Parameters.AddWithValue("@raddr", conn.RemoteAddress);
        cmd.Parameters.AddWithValue("@rport", conn.RemotePort);
        cmd.Parameters.AddWithValue("@state", conn.State);
        cmd.Parameters.AddWithValue("@proto", conn.Protocol);
        cmd.Parameters.AddWithValue("@dsent", conn.DataSent);
        cmd.Parameters.AddWithValue("@drecv", conn.DataReceived);
        cmd.Parameters.AddWithValue("@ts", conn.Timestamp.ToString("o"));
        cmd.Parameters.AddWithValue("@susp", conn.IsSuspicious ? 1 : 0);
        cmd.Parameters.AddWithValue("@sreason", conn.SuspiciousReason);
        cmd.ExecuteNonQuery();
    }

    public void InsertTrafficLog(TrafficLog log)
    {
        if (_connection == null) return;

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO TrafficLogs (ProcessName, ProcessId, RemoteAddress, RemotePort, Action, BytesTransferred, Timestamp, Details, IsSuspicious)
            VALUES (@pname, @pid, @raddr, @rport, @action, @bytes, @ts, @details, @susp)";
        cmd.Parameters.AddWithValue("@pname", log.ProcessName);
        cmd.Parameters.AddWithValue("@pid", log.ProcessId);
        cmd.Parameters.AddWithValue("@raddr", log.RemoteAddress);
        cmd.Parameters.AddWithValue("@rport", log.RemotePort);
        cmd.Parameters.AddWithValue("@action", log.Action);
        cmd.Parameters.AddWithValue("@bytes", log.BytesTransferred);
        cmd.Parameters.AddWithValue("@ts", log.Timestamp.ToString("o"));
        cmd.Parameters.AddWithValue("@details", log.Details);
        cmd.Parameters.AddWithValue("@susp", log.IsSuspicious ? 1 : 0);
        cmd.ExecuteNonQuery();
    }

    public List<TrafficLog> GetTrafficLogs(int limit = 500)
    {
        var logs = new List<TrafficLog>();
        if (_connection == null) return logs;

        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM TrafficLogs ORDER BY Timestamp DESC LIMIT @limit";
        cmd.Parameters.AddWithValue("@limit", limit);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            logs.Add(new TrafficLog
            {
                Id = reader.GetInt64(0),
                ProcessName = reader.GetString(1),
                ProcessId = reader.GetInt32(2),
                RemoteAddress = reader.GetString(3),
                RemotePort = reader.GetInt32(4),
                Action = reader.GetString(5),
                BytesTransferred = reader.GetInt64(6),
                Timestamp = DateTime.Parse(reader.GetString(7)),
                Details = reader.GetString(8),
                IsSuspicious = reader.GetInt32(9) == 1
            });
        }
        return logs;
    }

    public int DeleteOldLogs(TimeSpan age)
    {
        if (_connection == null) return 0;

        var cutoff = DateTime.Now.Subtract(age).ToString("o");

        using var cmd1 = _connection.CreateCommand();
        cmd1.CommandText = "DELETE FROM Connections WHERE Timestamp < @cutoff";
        cmd1.Parameters.AddWithValue("@cutoff", cutoff);
        var deleted1 = cmd1.ExecuteNonQuery();

        using var cmd2 = _connection.CreateCommand();
        cmd2.CommandText = "DELETE FROM TrafficLogs WHERE Timestamp < @cutoff";
        cmd2.Parameters.AddWithValue("@cutoff", cutoff);
        var deleted2 = cmd2.ExecuteNonQuery();

        // Reclaim disk space
        using var vacuumCmd = _connection.CreateCommand();
        vacuumCmd.CommandText = "VACUUM";
        vacuumCmd.ExecuteNonQuery();

        return deleted1 + deleted2;
    }

    public (int totalConnections, int totalLogs) GetRecordCounts()
    {
        if (_connection == null) return (0, 0);

        using var cmd1 = _connection.CreateCommand();
        cmd1.CommandText = "SELECT COUNT(*) FROM Connections";
        var connCount = Convert.ToInt32(cmd1.ExecuteScalar());

        using var cmd2 = _connection.CreateCommand();
        cmd2.CommandText = "SELECT COUNT(*) FROM TrafficLogs";
        var logCount = Convert.ToInt32(cmd2.ExecuteScalar());

        return (connCount, logCount);
    }

    public void Dispose()
    {
        var conn = _connection;
        _connection = null;
        conn?.Close();
        conn?.Dispose();
    }
}
