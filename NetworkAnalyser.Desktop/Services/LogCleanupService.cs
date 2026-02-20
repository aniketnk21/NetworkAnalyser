namespace NetworkAnalyser.Desktop.Services;

/// <summary>
/// Periodically deletes log entries older than 30 minutes from the SQLite database.
/// </summary>
public class LogCleanupService : IDisposable
{
    private Timer? _cleanupTimer;
    private readonly DatabaseService _db;
    private readonly TimeSpan _maxAge;
    private readonly TimeSpan _cleanupInterval;

    public event Action<int>? LogsCleaned;
    public event Action<string>? ErrorOccurred;
    public DateTime? LastCleanupTime { get; private set; }

    public LogCleanupService(DatabaseService db, TimeSpan? maxAge = null, TimeSpan? cleanupInterval = null)
    {
        _db = db;
        _maxAge = maxAge ?? TimeSpan.FromMinutes(30);
        _cleanupInterval = cleanupInterval ?? TimeSpan.FromMinutes(5); // check every 5 min
    }

    public void Start()
    {
        _cleanupTimer = new Timer(DoCleanup, null, TimeSpan.FromMinutes(1), _cleanupInterval);
    }

    private void DoCleanup(object? state)
    {
        try
        {
            var deletedCount = _db.DeleteOldLogs(_maxAge);
            LastCleanupTime = DateTime.Now;
            if (deletedCount > 0)
            {
                LogsCleaned?.Invoke(deletedCount);
            }
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke($"Cleanup error: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _cleanupTimer?.Change(Timeout.Infinite, Timeout.Infinite);
        _cleanupTimer?.Dispose();
    }
}
