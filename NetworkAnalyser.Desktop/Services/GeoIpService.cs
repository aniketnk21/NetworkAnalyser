using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using MaxMind.GeoIP2;

namespace NetworkAnalyser.Desktop.Services;

/// <summary>
/// Resolves IP addresses to country names.
/// Uses MaxMind GeoLite2 database when available, otherwise falls back to ip-api.com (free, no key required).
/// </summary>
public class GeoIpService : IDisposable
{
    private DatabaseReader? _reader;
    private readonly ConcurrentDictionary<string, string> _cache = new();
    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(3) };
    private bool _dbAvailable;

    // Private / reserved IP prefixes
    private static readonly string[] PrivatePrefixes =
        { "10.", "172.16.", "172.17.", "172.18.", "172.19.",
          "172.20.", "172.21.", "172.22.", "172.23.", "172.24.",
          "172.25.", "172.26.", "172.27.", "172.28.", "172.29.",
          "172.30.", "172.31.", "192.168.", "127.", "0." };

    public void Initialize(string? dbPath = null)
    {
        dbPath ??= Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "GeoLite2-Country.mmdb");

        if (File.Exists(dbPath))
        {
            try
            {
                _reader = new DatabaseReader(dbPath);
                _dbAvailable = true;
            }
            catch
            {
                _dbAvailable = false;
            }
        }
    }

    /// <summary>
    /// Returns the country name for an IP address.
    /// Returns "Local" for private IPs, or "Unknown" on lookup failure.
    /// </summary>
    public string GetCountry(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress) || ipAddress == "0.0.0.0")
            return "—";

        if (_cache.TryGetValue(ipAddress, out var cached))
            return cached;

        var result = ResolveCountry(ipAddress);
        _cache.TryAdd(ipAddress, result);
        return result;
    }

    private string ResolveCountry(string ipAddress)
    {
        // Check for private / loopback addresses
        foreach (var prefix in PrivatePrefixes)
        {
            if (ipAddress.StartsWith(prefix))
                return "Local";
        }

        // Try offline database first
        if (_dbAvailable && _reader != null)
        {
            try
            {
                if (IPAddress.TryParse(ipAddress, out var ip) &&
                    _reader.TryCountry(ip, out var response) &&
                    response?.Country?.Name != null)
                {
                    return response.Country.Name;
                }
            }
            catch { /* fall through to API */ }
        }

        // Fallback: free ip-api.com lookup
        try
        {
            var json = _http.GetStringAsync($"http://ip-api.com/json/{ipAddress}?fields=status,country").Result;
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("status", out var status) && status.GetString() == "success" &&
                root.TryGetProperty("country", out var country))
            {
                return country.GetString() ?? "Unknown";
            }
        }
        catch { /* ignore API errors */ }

        return "Unknown";
    }

    public void Dispose()
    {
        _reader?.Dispose();
        _http.Dispose();
    }
}
