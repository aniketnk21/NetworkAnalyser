# NetworkAnalyser

A real-time network traffic monitoring application for Windows that tracks active TCP connections, identifies suspicious activity, and provides detailed insights into network behavior.

## Features

- **Real-time Monitoring**: Track active TCP connections with live updates
- **Process Tracking**: Monitor which applications are making network connections
- **Geographic Location**: Identify the country of remote servers using GeoIP2
- **Suspicious Activity Detection**: Automatically flag unusual ports and potentially malicious connections
- **Traffic Logging**: Comprehensive logging of all network activity with SQLite database
- **Auto-cleanup**: Automatic log cleanup every 30 minutes to manage database size
- **Interactive UI**: Modern WPF interface with tabbed views and dashboard metrics

## Requirements

- Windows OS
- .NET 8.0 Runtime
- Administrator privileges (required for network monitoring)

## Installation

1. Download the latest release from the releases page
2. Run the installer (`NetworkAnalyserSetup.exe`)
3. Launch the application with administrator privileges

## Usage

1. **Start Monitoring**: Click the "▶ Start Monitoring" button to begin tracking network connections
2. **View Active Connections**: See real-time TCP connections in the "Active Connections" tab
3. **Review Traffic Logs**: Check historical network activity in the "Traffic Logs" tab
4. **Check Suspicious Activity**: Monitor flagged connections in the "Suspicious" tab
5. **Clear Logs**: Use the "Clear" button to remove all logged data

## Dashboard Metrics

- **Active Connections**: Number of live TCP connections
- **Unique Processes**: Count of applications with network activity
- **Suspicious**: Number of flagged connections
- **Log Entries**: Total logged network events

## Technologies

- **Framework**: .NET 8.0 with WPF
- **Database**: SQLite (Microsoft.Data.Sqlite)
- **GeoIP**: MaxMind GeoIP2
- **MVVM**: CommunityToolkit.Mvvm

## License

See [LICENSE](LICENSE) file for details.
