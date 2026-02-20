using System.Runtime.InteropServices;

namespace NetworkAnalyser.Desktop.Helpers;

/// <summary>
/// P/Invoke declarations for Windows IP Helper API to enumerate TCP connections with owning PIDs.
/// </summary>
public static class NativeMethods
{
    public const int AF_INET = 2;
    public const int TCP_TABLE_OWNER_PID_ALL = 5;

    [DllImport("iphlpapi.dll", SetLastError = true)]
    public static extern uint GetExtendedTcpTable(
        IntPtr pTcpTable,
        ref int dwOutBufLen,
        bool sort,
        int ipVersion,
        int tblClass,
        uint reserved = 0);

    [StructLayout(LayoutKind.Sequential)]
    public struct MIB_TCPTABLE_OWNER_PID
    {
        public uint dwNumEntries;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MIB_TCPROW_OWNER_PID
    {
        public uint state;
        public uint localAddr;
        public byte localPort1;
        public byte localPort2;
        public byte localPort3;
        public byte localPort4;
        public uint remoteAddr;
        public byte remotePort1;
        public byte remotePort2;
        public byte remotePort3;
        public byte remotePort4;
        public int owningPid;

        public System.Net.IPAddress LocalAddress =>
            new(localAddr);

        public ushort LocalPort =>
            BitConverter.ToUInt16(new[] { localPort2, localPort1 }, 0);

        public System.Net.IPAddress RemoteAddress =>
            new(remoteAddr);

        public ushort RemotePort =>
            BitConverter.ToUInt16(new[] { remotePort2, remotePort1 }, 0);

        public string StateString => state switch
        {
            1 => "CLOSED",
            2 => "LISTEN",
            3 => "SYN_SENT",
            4 => "SYN_RCVD",
            5 => "ESTABLISHED",
            6 => "FIN_WAIT1",
            7 => "FIN_WAIT2",
            8 => "CLOSE_WAIT",
            9 => "CLOSING",
            10 => "LAST_ACK",
            11 => "TIME_WAIT",
            12 => "DELETE_TCB",
            _ => "UNKNOWN"
        };
    }
}
